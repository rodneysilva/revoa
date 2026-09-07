using MongoDB.Bson;
using MongoDB.Driver;
using Revoa.Community.Domain.Aggregates.PostLikeAggregate;
using Revoa.Community.Domain.Repositories;
using Revoa.Infrastructure.Persistence;

namespace Revoa.Community.Infrastructure.Persistence;

public class PostLikesRepository : MongoRepositoryBase<PostLike>, IPostLikeRepository, IMongoIndexEnsurer
{
    private const int UserCap = 500;

    public PostLikesRepository(IMongoDatabase database) : base(database, "PostLikes")
    {
    }

    public Task<bool> ExistsAsync(Guid postId, Guid userId, CancellationToken ct)
    {
        var fb = Builders<PostLike>.Filter;
        return Collection.Find(fb.Eq(l => l.PostId, postId) & fb.Eq(l => l.UserId, userId))
            .AnyAsync(ct);
    }

    public Task AddAsync(PostLike like, CancellationToken ct)
    {
        _ = like ?? throw new ArgumentNullException(nameof(like));
        return Collection.InsertOneAsync(like, cancellationToken: ct);
    }

    public Task RemoveAsync(Guid postId, Guid userId, CancellationToken ct)
    {
        var fb = Builders<PostLike>.Filter;
        return Collection.DeleteOneAsync(
            fb.Eq(l => l.PostId, postId) & fb.Eq(l => l.UserId, userId), ct);
    }

    // Aggregation $match In(PostId) + $group {_id: $PostId, Count: $sum} —
    // mesmo formato de PostsRepository.GetChildrenCountsAsync (batch, anti-N+1).
    public async Task<IReadOnlyDictionary<Guid, int>> GetCountsAsync(
        IReadOnlyCollection<Guid> postIds, CancellationToken ct)
    {
        var dict = new Dictionary<Guid, int>();
        if (postIds.Count == 0)
        {
            return dict;
        }

        var fb = Builders<PostLike>.Filter;
        var grouped = await Collection.Aggregate()
            .Match(fb.In(l => l.PostId, postIds))
            .Group(new BsonDocument
            {
                { "_id", "$PostId" },
                { "Count", new BsonDocument("$sum", 1) }
            })
            .ToListAsync(ct);

        foreach (var doc in grouped)
        {
            dict[doc["_id"].AsGuid] = doc["Count"].AsInt32;
        }

        return dict;
    }

    public async Task<IReadOnlyCollection<Guid>> GetLikedPostIdsAsync(
        Guid userId, IReadOnlyCollection<Guid> postIds, CancellationToken ct)
    {
        if (postIds.Count == 0)
        {
            return Array.Empty<Guid>();
        }

        var fb = Builders<PostLike>.Filter;
        return await Collection.Find(fb.Eq(l => l.UserId, userId) & fb.In(l => l.PostId, postIds))
            .Project(l => l.PostId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Guid>> GetAllLikedIdsAsync(Guid userId, CancellationToken ct)
    {
        var fb = Builders<PostLike>.Filter;
        return await Collection.Find(fb.Eq(l => l.UserId, userId))
            .SortByDescending(l => l.CreatedAt)
            .Limit(UserCap)
            .Project(l => l.PostId)
            .ToListAsync(ct);
    }

    /// <summary>Índice único por (post, usuário) — toggle idempotente. Idempotente.</summary>
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await Collection.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<PostLike>(
                Builders<PostLike>.IndexKeys
                    .Ascending(l => l.PostId)
                    .Ascending(l => l.UserId),
                new CreateIndexOptions { Name = "ux_Post_User", Unique = true }),
            new CreateIndexModel<PostLike>(
                Builders<PostLike>.IndexKeys.Ascending(l => l.UserId),
                new CreateIndexOptions { Name = "ix_User" })
        }, ct);
    }
}
