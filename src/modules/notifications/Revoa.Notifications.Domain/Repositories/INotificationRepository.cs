using Revoa.Notifications.Domain.Aggregates.NotificationAggregate;

namespace Revoa.Notifications.Domain.Repositories;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct);

    // Ordenada por CreatedAt desc; unreadOnly filtra só não-lidas.
    Task<IReadOnlyList<Notification>> GetByUserAsync(
        Guid userId, bool unreadOnly, int limit, int skip, CancellationToken ct);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct);

    Task AddAsync(Notification notification, CancellationToken ct);

    Task UpdateAsync(Notification notification, CancellationToken ct);

    Task EnsureIndexesAsync(CancellationToken ct = default);
}
