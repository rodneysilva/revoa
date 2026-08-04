using Revoa.Notifications.Domain.Aggregates.PushSubscriptionAggregate;

namespace Revoa.Notifications.Application.Services;

// Porta de envio Web Push (anti-corruption entre Notifier e a library WebPush).
// Impl em Infrastructure (WebPushService). Resiliente: falhas isoladas por subscription.
public interface IWebPushSender
{
    Task SendAsync(
        IReadOnlyList<PushSubscription> subscriptions,
        string title,
        string body,
        string? payload,
        CancellationToken ct = default);
}
