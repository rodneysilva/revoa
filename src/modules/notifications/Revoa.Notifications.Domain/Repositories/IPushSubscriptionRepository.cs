using Revoa.Notifications.Domain.Aggregates.PushSubscriptionAggregate;

namespace Revoa.Notifications.Domain.Repositories;

public interface IPushSubscriptionRepository
{
    Task<IReadOnlyList<PushSubscription>> GetByUserAsync(Guid userId, CancellationToken ct);

    Task<PushSubscription?> GetByEndpointAsync(string endpoint, CancellationToken ct);

    // Insere se Endpoint novo, senão atualiza o documento (IsUpsert por Endpoint).
    Task UpsertAsync(PushSubscription subscription, CancellationToken ct);

    Task DeleteByEndpointAsync(string endpoint, CancellationToken ct);

    Task EnsureIndexesAsync(CancellationToken ct = default);
}
