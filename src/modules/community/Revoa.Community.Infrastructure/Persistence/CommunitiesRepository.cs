using MongoDB.Driver;
using Revoa.Abstractions;
using Revoa.Community.Domain.Aggregates.CommunityAggregate;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Infrastructure.Persistence;

public class CommunitiesRepository : ICommunityRepository
{
    private readonly IMongoCollection<CommunityGroup> _communities;

    public CommunitiesRepository(IMongoDatabase database)
    {
        _communities = database.GetCollection<CommunityGroup>("Communities");
    }

    public async Task<CommunityGroup?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _communities.Find(c => c.Id == id).FirstOrDefaultAsync(ct);
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

        return await _communities.Find(query)
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

        return await _communities.Find(query).FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(CommunityGroup community, CancellationToken ct)
    {
        await _communities.InsertOneAsync(community, cancellationToken: ct);
    }

    public async Task UpdateAsync(CommunityGroup community, CancellationToken ct)
    {
        var expectedVersion = community.Version;

        // Optimistic locking: _id + (Version == esperada OU doc legado sem Version).
        var filter = Builders<CommunityGroup>.Filter.Eq(c => c.Id, community.Id)
                     & (Builders<CommunityGroup>.Filter.Eq(c => c.Version, expectedVersion)
                        | Builders<CommunityGroup>.Filter.Exists(c => c.Version, false));

        community.IncrementVersion();

        var result = await _communities.ReplaceOneAsync(filter, community, cancellationToken: ct);
        if (result.MatchedCount == 0)
        {
            throw new ConcurrencyException(community.Id.ToString(), expectedVersion);
        }
    }

    /// <summary>
    /// Índices do feed de comunidades (Tipo+Cidade, Criador, geo, visibilidade). Idempotente.
    /// </summary>
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await _communities.Indexes.CreateManyAsync(new[]
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
