using MongoDB.Bson;
using MongoDB.Driver;
using Revoa.Community.Domain.Aggregates.MembershipAggregate;
using Revoa.Community.Domain.Repositories;
using Revoa.Infrastructure.Persistence;

namespace Revoa.Community.Infrastructure.Persistence;

public class MembershipsRepository : MongoRepositoryBase<Membership>, IMembershipRepository, IMongoIndexEnsurer
{
    public MembershipsRepository(IMongoDatabase database) : base(database, "Memberships")
    {
    }

    public async Task<Membership?> GetByUserAndCommunityAsync(Guid userId, Guid communityId, CancellationToken ct)
    {
        var fb = Builders<Membership>.Filter;
        return await Collection.Find(
            fb.Eq(m => m.UserId, userId) & fb.Eq(m => m.CommunityId, communityId))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<Membership>> ListByCommunityAsync(Guid communityId, CancellationToken ct)
    {
        return await Collection.Find(m => m.CommunityId == communityId)
            .SortBy(m => m.JoinedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Membership>> ListByUsuarioAsync(Guid userId, CancellationToken ct)
    {
        return await Collection.Find(m => m.UserId == userId)
            .SortByDescending(m => m.JoinedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> CountActiveByCommunityAsync(
        IEnumerable<Guid> comunidadeIds, CancellationToken ct)
    {
        var ids = comunidadeIds.ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var fb = Builders<Membership>.Filter;
        var query = fb.In(m => m.CommunityId, ids) & fb.Eq(m => m.Status, MembershipStatus.Active);

        var matchDoc = query.Render(Collection.DocumentSerializer, Collection.Settings.SerializerRegistry);

        var pipeline = new BsonDocument[]
        {
            new() { ["$match"] = matchDoc },
            new() { ["$group"] = new BsonDocument { ["_id"] = "$CommunityId", ["count"] = new BsonDocument("$sum", 1) } }
        };

        var result = new Dictionary<Guid, int>();
        using var cursor = await Collection.AggregateAsync<BsonDocument>(pipeline, cancellationToken: ct);
        await cursor.ForEachAsync(doc =>
        {
            var communityId = doc["_id"].AsGuid;
            result[communityId] = doc["count"].AsInt32;
        }, ct);

        return result;
    }

    public async Task DeleteAsync(Membership membership, CancellationToken ct)
    {
        await Collection.DeleteOneAsync(m => m.Id == membership.Id, ct);
    }

    /// <summary>
    /// Índice único composto (UserId, CommunityId) — garante 1 membership por usuário/comunidade.
    /// </summary>
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await Collection.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Membership>(
                Builders<Membership>.IndexKeys
                    .Ascending(m => m.UserId)
                    .Ascending(m => m.CommunityId),
                new CreateIndexOptions { Name = "ux_Usuario_Comunidade", Unique = true }),
            new CreateIndexModel<Membership>(
                Builders<Membership>.IndexKeys.Ascending(m => m.CommunityId),
                new CreateIndexOptions { Name = "ix_ComunidadeId" })
        }, ct);
    }
}
