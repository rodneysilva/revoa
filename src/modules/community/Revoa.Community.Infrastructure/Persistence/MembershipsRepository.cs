using MongoDB.Bson;
using MongoDB.Driver;
using Revoa.Abstractions;
using Revoa.Community.Domain.Aggregates.MembershipAggregate;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Infrastructure.Persistence;

public class MembershipsRepository : IMembershipRepository
{
    private readonly IMongoCollection<Membership> _memberships;

    public MembershipsRepository(IMongoDatabase database)
    {
        _memberships = database.GetCollection<Membership>("Memberships");
    }

    public async Task<Membership?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _memberships.Find(m => m.Id == id).FirstOrDefaultAsync(ct);
    }

    public async Task<Membership?> GetByUsuarioEComunidadeAsync(Guid usuarioId, Guid comunidadeId, CancellationToken ct)
    {
        var fb = Builders<Membership>.Filter;
        return await _memberships.Find(
            fb.Eq(m => m.UsuarioId, usuarioId) & fb.Eq(m => m.ComunidadeId, comunidadeId))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<Membership>> ListByComunidadeAsync(Guid comunidadeId, CancellationToken ct)
    {
        return await _memberships.Find(m => m.ComunidadeId == comunidadeId)
            .SortBy(m => m.JoinedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Membership>> ListByUsuarioAsync(Guid usuarioId, CancellationToken ct)
    {
        return await _memberships.Find(m => m.UsuarioId == usuarioId)
            .SortByDescending(m => m.JoinedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> CountAtivasByComunidadeAsync(
        IEnumerable<Guid> comunidadeIds, CancellationToken ct)
    {
        var ids = comunidadeIds.ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var fb = Builders<Membership>.Filter;
        var query = fb.In(m => m.ComunidadeId, ids) & fb.Eq(m => m.Status, MembershipStatus.Ativa);

        var matchDoc = query.Render(_memberships.DocumentSerializer, _memberships.Settings.SerializerRegistry);

        var pipeline = new BsonDocument[]
        {
            new() { ["$match"] = matchDoc },
            new() { ["$group"] = new BsonDocument { ["_id"] = "$ComunidadeId", ["count"] = new BsonDocument("$sum", 1) } }
        };

        var result = new Dictionary<Guid, int>();
        using var cursor = await _memberships.AggregateAsync<BsonDocument>(pipeline, cancellationToken: ct);
        await cursor.ForEachAsync(doc =>
        {
            var comunidadeId = doc["_id"].AsGuid;
            result[comunidadeId] = doc["count"].AsInt32;
        }, ct);

        return result;
    }

    public async Task AddAsync(Membership membership, CancellationToken ct)
    {
        await _memberships.InsertOneAsync(membership, cancellationToken: ct);
    }

    public async Task UpdateAsync(Membership membership, CancellationToken ct)
    {
        var expectedVersion = membership.Version;

        var filter = Builders<Membership>.Filter.Eq(m => m.Id, membership.Id)
                     & (Builders<Membership>.Filter.Eq(m => m.Version, expectedVersion)
                        | Builders<Membership>.Filter.Exists(m => m.Version, false));

        membership.IncrementVersion();

        var result = await _memberships.ReplaceOneAsync(filter, membership, cancellationToken: ct);
        if (result.MatchedCount == 0)
        {
            throw new ConcurrencyException(membership.Id.ToString(), expectedVersion);
        }
    }

    public async Task DeleteAsync(Membership membership, CancellationToken ct)
    {
        await _memberships.DeleteOneAsync(m => m.Id == membership.Id, ct);
    }

    /// <summary>
    /// Índice único composto (UsuarioId, ComunidadeId) — garante 1 membership por usuário/comunidade.
    /// </summary>
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await _memberships.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Membership>(
                Builders<Membership>.IndexKeys
                    .Ascending(m => m.UsuarioId)
                    .Ascending(m => m.ComunidadeId),
                new CreateIndexOptions { Name = "ux_Usuario_Comunidade", Unique = true }),
            new CreateIndexModel<Membership>(
                Builders<Membership>.IndexKeys.Ascending(m => m.ComunidadeId),
                new CreateIndexOptions { Name = "ix_ComunidadeId" })
        }, ct);
    }
}
