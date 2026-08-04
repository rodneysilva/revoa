using MongoDB.Driver;
using Revoa.Abstractions;
using Revoa.Notifications.Domain.Aggregates.NotificationAggregate;
using Revoa.Notifications.Domain.Repositories;

namespace Revoa.Notifications.Infrastructure.Persistence;

public class NotificationsRepository : INotificationRepository
{
    private readonly IMongoCollection<Notification> _notifications;

    public NotificationsRepository(IMongoDatabase database)
    {
        _notifications = database.GetCollection<Notification>("Notifications");
    }

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _notifications.Find(n => n.Id == id).FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<Notification>> GetByUserAsync(
        Guid userId, bool unreadOnly, int limit, int skip, CancellationToken ct)
    {
        var fb = Builders<Notification>.Filter;
        var filter = fb.Eq(n => n.UserId, userId);
        if (unreadOnly)
        {
            filter &= fb.Eq(n => n.Lida, false);
        }

        return await _notifications.Find(filter)
            .SortByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync(ct);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct)
    {
        var fb = Builders<Notification>.Filter;
        var filter = fb.Eq(n => n.UserId, userId) & fb.Eq(n => n.Lida, false);
        return (int)await _notifications.CountDocumentsAsync(filter, cancellationToken: ct);
    }

    public async Task AddAsync(Notification notification, CancellationToken ct)
    {
        await _notifications.InsertOneAsync(notification, cancellationToken: ct);
    }

    // Optimistic locking: _id + (Version == esperada | Version ausente p/ legados).
    public async Task UpdateAsync(Notification notification, CancellationToken ct)
    {
        var expectedVersion = notification.Version;

        var filter = Builders<Notification>.Filter.Eq(n => n.Id, notification.Id)
                     & (Builders<Notification>.Filter.Eq(n => n.Version, expectedVersion)
                        | Builders<Notification>.Filter.Exists(n => n.Version, false));

        notification.IncrementVersion();

        var result = await _notifications.ReplaceOneAsync(filter, notification, cancellationToken: ct);
        if (result.MatchedCount == 0)
        {
            throw new ConcurrencyException(notification.Id.ToString(), expectedVersion);
        }
    }

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await _notifications.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Notification>(
                Builders<Notification>.IndexKeys
                    .Ascending(n => n.UserId)
                    .Descending(n => n.CreatedAt),
                new CreateIndexOptions { Name = "ix_UserId_CreatedAt" }),
            new CreateIndexModel<Notification>(
                Builders<Notification>.IndexKeys
                    .Ascending(n => n.UserId)
                    .Ascending(n => n.Lida),
                new CreateIndexOptions { Name = "ix_UserId_Lida" })
        }, ct);
    }
}
