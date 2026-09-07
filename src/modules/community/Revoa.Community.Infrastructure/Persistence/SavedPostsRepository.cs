using MongoDB.Driver;
using Revoa.Community.Domain.Aggregates.SavedPostAggregate;
using Revoa.Community.Domain.Repositories;
using Revoa.Infrastructure.Persistence;

namespace Revoa.Community.Infrastructure.Persistence;

public class SavedPostsRepository : MongoRepositoryBase<SavedPost>, ISavedPostRepository, IMongoIndexEnsurer
{
    private const int UserCap = 200;

    public SavedPostsRepository(IMongoDatabase database) : base(database, "SavedPosts")
    {
    }

    public Task<bool> ExistsAsync(Guid userId, Guid postId, CancellationToken ct)
    {
        var fb = Builders<SavedPost>.Filter;
        return Collection.Find(fb.Eq(s => s.UserId, userId) & fb.Eq(s => s.PostId, postId))
            .AnyAsync(ct);
    }

    public Task AddAsync(SavedPost saved, CancellationToken ct)
    {
        _ = saved ?? throw new ArgumentNullException(nameof(saved));
        return Collection.InsertOneAsync(saved, cancellationToken: ct);
    }

    public Task RemoveAsync(Guid userId, Guid postId, CancellationToken ct)
    {
        var fb = Builders<SavedPost>.Filter;
        return Collection.DeleteOneAsync(
            fb.Eq(s => s.UserId, userId) & fb.Eq(s => s.PostId, postId), ct);
    }

    public async Task<IReadOnlyList<SavedPost>> GetByUserAsync(Guid userId, CancellationToken ct)
    {
        var fb = Builders<SavedPost>.Filter;
        return await Collection.Find(fb.Eq(s => s.UserId, userId))
            .SortByDescending(s => s.CreatedAt)
            .Limit(UserCap)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyCollection<Guid>> GetSavedPostIdsAsync(
        Guid userId, IReadOnlyCollection<Guid> postIds, CancellationToken ct)
    {
        if (postIds.Count == 0)
        {
            return Array.Empty<Guid>();
        }

        var fb = Builders<SavedPost>.Filter;
        return await Collection.Find(fb.Eq(s => s.UserId, userId) & fb.In(s => s.PostId, postIds))
            .Project(s => s.PostId)
            .ToListAsync(ct);
    }

    /// <summary>Índice único por (usuário, post) — toggle idempotente. Idempotente.</summary>
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await Collection.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<SavedPost>(
                Builders<SavedPost>.IndexKeys
                    .Ascending(s => s.UserId)
                    .Ascending(s => s.PostId),
                new CreateIndexOptions { Name = "ux_User_Post", Unique = true }),
            new CreateIndexModel<SavedPost>(
                Builders<SavedPost>.IndexKeys.Ascending(s => s.CommunityId),
                new CreateIndexOptions { Name = "ix_Community" })
        }, ct);
    }
}
