using MongoDB.Driver;
using Revoa.Community.Domain.Aggregates.CommunityAggregate;
using Revoa.Community.Domain.Repositories;
using Revoa.Infrastructure.Persistence;

namespace Revoa.Community.Infrastructure.Persistence;

public class CommunitiesRepository : MongoRepositoryBase<CommunityGroup>, ICommunityRepository, IMongoIndexEnsurer
{
    public CommunitiesRepository(IMongoDatabase database) : base(database, "Communities")
    {
    }

    public async Task<IReadOnlyList<CommunityGroup>> GetPublicAsync(PublicFilter filter, CancellationToken ct)
    {
        var fb = Builders<CommunityGroup>.Filter;

        // Ativas E (Default OU (User e Open)).
        var query = fb.Eq(c => c.Status, CommunityStatus.Active)
                    & (fb.Eq(c => c.Tipo, CommunityTipo.Default)
                       | fb.And(
                           fb.Eq(c => c.Tipo, CommunityTipo.User),
                           fb.Eq(c => c.Visibilidade, CommunityVisibilidade.Open)));

        if (filter.Eixo is not null)
        {
            query &= fb.Eq(c => c.Eixo, filter.Eixo);
        }

        if (!string.IsNullOrWhiteSpace(filter.Cidade))
        {
            query &= fb.Eq(c => c.Cidade, filter.Cidade);
        }

        var limit = filter.Limit > 0 ? filter.Limit : 100;

        return await Collection.Find(query)
            .SortByDescending(c => c.Version)
            .Limit(limit)
            .ToListAsync(ct);
    }

    public async Task<CommunityGroup?> GetByCidadeETipoDefaultAsync(string cidade, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cidade))
        {
            return null;
        }

        var fb = Builders<CommunityGroup>.Filter;
        var query = fb.Eq(c => c.Tipo, CommunityTipo.Default)
                    & fb.Eq(c => c.Cidade, cidade)
                    & fb.Eq(c => c.Status, CommunityStatus.Active);

        return await Collection.Find(query).FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Índices do feed de comunidades (Tipo+Cidade, Criador, geo, visibilidade). Idempotente.
    /// </summary>
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await Collection.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<CommunityGroup>(
                Builders<CommunityGroup>.IndexKeys
                    .Ascending(c => c.Tipo)
                    .Ascending(c => c.Cidade),
                new CreateIndexOptions { Name = "ix_Tipo_Cidade", Sparse = true }),
            new CreateIndexModel<CommunityGroup>(
                Builders<CommunityGroup>.IndexKeys.Ascending(c => c.CriadorId),
                new CreateIndexOptions { Name = "ix_CriadorId" }),
            new CreateIndexModel<CommunityGroup>(
                Builders<CommunityGroup>.IndexKeys
                    .Ascending(c => c.Lat)
                    .Ascending(c => c.Lng),
                new CreateIndexOptions { Name = "ix_Localizacao_Lat_Lng", Sparse = true }),
            new CreateIndexModel<CommunityGroup>(
                Builders<CommunityGroup>.IndexKeys.Ascending(c => c.Visibilidade),
                new CreateIndexOptions { Name = "ix_Visibilidade" })
        }, ct);
    }
}
