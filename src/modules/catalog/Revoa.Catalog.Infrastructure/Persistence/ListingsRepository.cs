using MongoDB.Driver;
using Revoa.Abstractions;
using Revoa.Catalog.Domain.Aggregates.ListingAggregate;
using Revoa.Catalog.Domain.Repositories;

namespace Revoa.Catalog.Infrastructure.Persistence;

public class ListingsRepository : IListingRepository
{
    private readonly IMongoCollection<Listing> _listings;

    public ListingsRepository(IMongoDatabase database)
    {
        _listings = database.GetCollection<Listing>("Listings");
    }

    public async Task<Listing?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _listings.Find(l => l.Id == id).FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<Listing>> GetFeedAsync(FeedFilter filter, CancellationToken ct)
    {
        var fb = Builders<Listing>.Filter;

        // Só anúncios ativos.
        var query = fb.Eq(l => l.Status, ListingStatus.Ativo);

        // Visibilidade: Global/Ambos sempre aparecem. Comunidade só se ComunidadeId bate.
        // Sem ComunidadeId no filtro → exclui os escopados a comunidade.
        if (filter.ComunidadeId is not null)
        {
            var comunidadeOuGlobal = fb.And(
                fb.Ne(l => l.Visibilidade, ListingVisibilidade.Comunidade)) // Global/Ambos
                | fb.And(
                    fb.Eq(l => l.Visibilidade, ListingVisibilidade.Comunidade),
                    fb.Eq(l => l.ComunidadeId, filter.ComunidadeId));
            query &= comunidadeOuGlobal;
        }
        else
        {
            query &= fb.Ne(l => l.Visibilidade, ListingVisibilidade.Comunidade);
        }

        if (filter.Kind is not null)
        {
            query &= fb.Eq(l => l.Kind, filter.Kind);
        }

        if (filter.CategoriaId is not null)
        {
            query &= fb.Eq(l => l.CategoriaId, filter.CategoriaId);
        }

        var limit = filter.Limit > 0 ? filter.Limit : 100;

        return await _listings.Find(query)
            .SortByDescending(l => l.Version)
            .Limit(limit)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Listing listing, CancellationToken ct)
    {
        await _listings.InsertOneAsync(listing, cancellationToken: ct);
    }

    public async Task UpdateAsync(Listing listing, CancellationToken ct)
    {
        var expectedVersion = listing.Version;

        // Optimistic locking: _id + (Version == esperada OU doc legado sem Version).
        var filter = Builders<Listing>.Filter.Eq(l => l.Id, listing.Id)
                     & (Builders<Listing>.Filter.Eq(l => l.Version, expectedVersion)
                        | Builders<Listing>.Filter.Exists(l => l.Version, false));

        listing.IncrementVersion();

        var result = await _listings.ReplaceOneAsync(filter, listing, cancellationToken: ct);
        if (result.MatchedCount == 0)
        {
            throw new ConcurrencyException(listing.Id.ToString(), expectedVersion);
        }
    }

    /// <summary>
    /// Cria índices do feed (Status+Visibilidade, CategoriaId, ComunidadeId, Lat/Lng). Idempotente.
    /// </summary>
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await _listings.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Listing>(
                Builders<Listing>.IndexKeys
                    .Ascending(l => l.Status)
                    .Ascending(l => l.Visibilidade),
                new CreateIndexOptions { Name = "ix_Status_Visibilidade" }),
            new CreateIndexModel<Listing>(
                Builders<Listing>.IndexKeys.Ascending(l => l.CategoriaId),
                new CreateIndexOptions { Name = "ix_CategoriaId" }),
            new CreateIndexModel<Listing>(
                Builders<Listing>.IndexKeys.Ascending(l => l.ComunidadeId),
                new CreateIndexOptions { Name = "ix_ComunidadeId", Sparse = true }),
            new CreateIndexModel<Listing>(
                Builders<Listing>.IndexKeys
                    .Ascending("Localizacao.Lat")
                    .Ascending("Localizacao.Lng"),
                new CreateIndexOptions { Name = "ix_Localizacao_Lat_Lng", Sparse = true })
        }, ct);
    }
}
