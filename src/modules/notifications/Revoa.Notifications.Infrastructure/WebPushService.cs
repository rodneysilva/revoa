using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Revoa.Notifications.Application.Services;
using Revoa.Notifications.Domain.Aggregates.PushSubscriptionAggregate;

namespace Revoa.Notifications.Infrastructure;

// Envio Web Push (RFC 8291). MVP: STUB RESILIENTE — sem a library WebPush (risco de conflito
// BouncyCastle vs Nethereum; e Web Push real exige HTTPS + browser + VAPID, não testável aqui).
// A entrega primária/testável é o in-app SignalR (Notifier).
//
// Quando ativar a library WebPush: injetar WebPushClient, montar VapidDetails de VapidOptions e,
// por subscription, chamar SendNotification(endpoint,p256dh,auth) num try/catch isolado;
// resposta 404/410 → DeleteByEndpointAsync (inscrição expirada). Esse swapping é local a esta classe.
public class WebPushService : IWebPushSender
{
    private readonly VapidOptions _vapid;
    private readonly ILogger<WebPushService> _logger;

    public WebPushService(IOptions<VapidOptions> vapid, ILogger<WebPushService> logger)
    {
        _vapid = vapid.Value;
        _logger = logger;
    }

    public Task SendAsync(
        IReadOnlyList<PushSubscription> subscriptions,
        string title,
        string body,
        string? payload,
        CancellationToken ct = default)
    {
        if (subscriptions.Count == 0)
        {
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(_vapid.PublicKey) || string.IsNullOrWhiteSpace(_vapid.PrivateKey))
        {
            _logger.LogDebug(
                "Web Push desativado (sem chaves VAPID) — {Count} subscription(ões) não enviadas.",
                subscriptions.Count);
            return Task.CompletedTask;
        }

        var payloadJson = JsonSerializer.Serialize(new { title, body, payload });

        foreach (var sub in subscriptions)
        {
            // TODO(library WebPush): new PushSubscription(endpoint,p256dh,auth) + webPushClient.SendNotification.
            _logger.LogInformation("Web Push (stub): enviaria p/ {Endpoint} — {Payload}", sub.Endpoint, payloadJson);
        }

        return Task.CompletedTask;
    }
}
