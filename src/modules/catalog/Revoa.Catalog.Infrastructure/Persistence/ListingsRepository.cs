using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using Revoa.Catalog.Domain.Aggregates.ListingAggregate;
using Revoa.Catalog.Domain.Repositories;
using Revoa.Infrastructure.Persistence;

namespace Revoa.Catalog.Infrastructure.Persistence;

public class ListingsRepository : MongoRepositoryBase<Listing>, IListingRepository, IMongoIndexEnsurer
{
    public ListingsRepository(IMongoDatabase database) : base(database, "Listings")
    {
    }

    public async Task<IReadOnlyList<Listing>> GetFeedAsync(FeedFilter filter, CancellationToken ct)
    {
        var fb = Builders<Listing>.Filter;

        // Só anúncios ativos.
        var query = fb.Eq(l => l.Status, ListingStatus.Active);

        // Visibility: Global/Ambos sempre aparecem. Comunidade só se CommunityId bate.
        // Sem CommunityId no filtro → exclui os escopados a comunidade.
        if (filter.CommunityId is not null)
        {
            var comunidadeOuGlobal = fb.And(
                fb.Ne(l => l.Visibility, ListingVisibility.Community)) // Global/Ambos
                | fb.And(
                    fb.Eq(l => l.Visibility, ListingVisibility.Community),
                    fb.Eq(l => l.CommunityId, filter.CommunityId));
            query &= comunidadeOuGlobal;
        }
        else
        {
            query &= fb.Ne(l => l.Visibility, ListingVisibility.Community);
        }

        if (filter.Kind is not null)
        {
            query &= fb.Eq(l => l.Kind, filter.Kind);
        }

        if (filter.Mode is not null)
        {
            query &= fb.Eq(l => l.Mode, filter.Mode);
        }

        if (filter.CategoryId is not null)
        {
            query &= fb.Eq(l => l.CategoryId, filter.CategoryId);
        }

        if (filter.PriceMin is not null)
        {
            query &= fb.Gte(l => l.PriceRvm, filter.PriceMin.Value);
        }

        if (filter.PriceMax is not null)
        {
            query &= fb.Lte(l => l.PriceRvm, filter.PriceMax.Value);
        }

        if (filter.DonationOnly is true)
        {
            query &= fb.Eq(l => l.PriceRvm, 0L);
        }

        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var rx = new BsonRegularExpression(Regex.Escape(filter.Q.Trim()), "i");
            query &= fb.Regex(l => l.Title, rx) | fb.Regex(l => l.Description, rx);
        }

        if (filter.SellerIds is { Count: > 0 })
        {
            query &= fb.In(l => l.SellerId, filter.SellerIds);
        }

        var sort = filter.Sort switch
        {
            "preco-asc" => Builders<Listing>.Sort.Ascending(l => l.PriceRvm),
            "preco-desc" => Builders<Listing>.Sort.Descending(l => l.PriceRvm),
            _ => Builders<Listing>.Sort.Descending(l => l.CreatedAt)
        };

        var find = Collection.Find(query).Sort(sort);

        if (filter.RadiusMode)
        {
            var cap = filter.CandidateCap > 0 ? filter.CandidateCap : 200;
            return await find.Limit(cap).ToListAsync(ct);
        }

        var page = filter.Page <= 0 ? 1 : filter.Page;
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 24;

        return await find
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);
    }

    // Vários por id, só ativos (lista de Salvos — join com bookmarks do usuário).
    public async Task<IReadOnlyList<Listing>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        if (ids.Count == 0)
        {
            return Array.Empty<Listing>();
        }

        var fb = Builders<Listing>.Filter;
        return await Collection
            .Find(fb.In(l => l.Id, ids) & fb.Eq(l => l.Status, ListingStatus.Active))
            .ToListAsync(ct);
    }

    // Todos os anúncios ativos (sem paginação/geo). Usado pelo adapter de Pricing (mediana comunitária).
    public async Task<IReadOnlyList<Listing>> GetActiveAsync(CancellationToken ct)
    {
        return await Collection
            .Find(l => l.Status == ListingStatus.Active)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Cria índices do feed (Status+Visibilidade, CategoryId, CommunityId, Lat/Lng, CreatedAt). Idempotente.
    /// </summary>
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await Collection.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Listing>(
                Builders<Listing>.IndexKeys
                    .Ascending(l => l.Status)
                    .Ascending(l => l.Visibility),
                new CreateIndexOptions { Name = "ix_Status_Visibilidade" }),
            new CreateIndexModel<Listing>(
                Builders<Listing>.IndexKeys.Ascending(l => l.CategoryId),
                new CreateIndexOptions { Name = "ix_CategoriaId" }),
            new CreateIndexModel<Listing>(
                Builders<Listing>.IndexKeys.Ascending(l => l.CommunityId),
                new CreateIndexOptions { Name = "ix_ComunidadeId", Sparse = true }),
            new CreateIndexModel<Listing>(
                Builders<Listing>.IndexKeys
                    .Ascending("Localizacao.Lat")
                    .Ascending("Localizacao.Lng"),
                new CreateIndexOptions { Name = "ix_Localizacao_Lat_Lng", Sparse = true }),
            new CreateIndexModel<Listing>(
                Builders<Listing>.IndexKeys.Descending(l => l.CreatedAt),
                new CreateIndexOptions { Name = "ix_CreatedAt_Desc" }),
            new CreateIndexModel<Listing>(
                Builders<Listing>.IndexKeys.Ascending(l => l.PriceRvm),
                new CreateIndexOptions { Name = "ix_PrecoRvm" })
        }, ct);
    }
}
