using MongoDB.Driver;
using Revoa.Notifications.Domain.Aggregates.NotificationAggregate;
using Revoa.Notifications.Domain.Repositories;
using Revoa.Infrastructure.Persistence;

namespace Revoa.Notifications.Infrastructure.Persistence;

public class NotificationsRepository : MongoRepositoryBase<Notification>, INotificationRepository, IMongoIndexEnsurer
{
    public NotificationsRepository(IMongoDatabase database) : base(database, "Notifications")
    {
    }

    public async Task<IReadOnlyList<Notification>> GetByUserAsync(
        Guid userId, bool unreadOnly, int limit, int skip, CancellationToken ct)
    {
        var fb = Builders<Notification>.Filter;
        var filter = fb.Eq(n => n.UserId, userId);
        if (unreadOnly)
        {
            filter &= fb.Eq(n => n.Read, false);
        }

        return await Collection.Find(filter)
            .SortByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync(ct);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct)
    {
        var fb = Builders<Notification>.Filter;
        var filter = fb.Eq(n => n.UserId, userId) & fb.Eq(n => n.Read, false);
        return (int)await Collection.CountDocumentsAsync(filter, cancellationToken: ct);
    }

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await Collection.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Notification>(
                Builders<Notification>.IndexKeys
                    .Ascending(n => n.UserId)
                    .Descending(n => n.CreatedAt),
                new CreateIndexOptions { Name = "ix_UserId_CreatedAt" }),
            new CreateIndexModel<Notification>(
                Builders<Notification>.IndexKeys
                    .Ascending(n => n.UserId)
                    .Ascending(n => n.Read),
                new CreateIndexOptions { Name = "ix_UserId_Read" })
        }, ct);
    }
}
