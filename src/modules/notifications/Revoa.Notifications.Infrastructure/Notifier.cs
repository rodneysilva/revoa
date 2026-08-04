using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Revoa.IntegrationContracts.Notifications;
using Revoa.Notifications.Application.DTOs;
using Revoa.Notifications.Application.Services;
using Revoa.Notifications.Domain.Aggregates.NotificationAggregate;
using Revoa.Notifications.Domain.Repositories;
using Revoa.Notifications.Infrastructure.Hubs;

namespace Revoa.Notifications.Infrastructure;

// Implementa a porta INotifier (IntegrationContracts). Fluxo:
// (1) parse type → NotificationType (fallback System);
// (2) Notification.Create + AddAsync;
// (3) broadcast in-app via SignalR ao grupo "user-{userId}" (ReceiveNotification);
// (4) Web Push a todas as PushSubscriptions do usuário.
// Cada etapa é resiliente: uma falha não quebra a outra (o SignalR offline não derruba o push, etc.).
public class Notifier : INotifier
{
    private readonly INotificationRepository _notifications;
    private readonly IPushSubscriptionRepository _subscriptions;
    private readonly IHubContext<NotificationsHub> _hub;
    private readonly IWebPushSender _webPush;
    private readonly ILogger<Notifier> _logger;

    public Notifier(
        INotificationRepository notifications,
        IPushSubscriptionRepository subscriptions,
        IHubContext<NotificationsHub> hub,
        IWebPushSender webPush,
        ILogger<Notifier> logger)
    {
        _notifications = notifications;
        _subscriptions = subscriptions;
        _hub = hub;
        _webPush = webPush;
        _logger = logger;
    }

    public async Task NotifyAsync(
        Guid userId,
        string type,
        string title,
        string body,
        string? payloadJson = null,
        CancellationToken ct = default)
    {
        if (!Enum.TryParse<NotificationType>(type, ignoreCase: true, out var notificationType))
        {
            notificationType = NotificationType.System;
        }

        Notification notification;
        try
        {
            notification = Notification.Create(userId, notificationType, title, body, payloadJson);
            await _notifications.AddAsync(notification, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao persistir notificação p/ usuário {UserId}.", userId);
            return;
        }

        var dto = NotificationDtoMapper.From(notification);

        // Broadcast in-app (SignalR). Cliente offline = silencioso.
        try
        {
            await _hub.Clients.Group($"user-{userId}").SendAsync("ReceiveNotification", dto, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha no broadcast SignalR p/ usuário {UserId}.", userId);
        }

        // Web Push a todas as inscrições do dispositivo (falha isolada por subscription no serviço).
        try
        {
            var subs = await _subscriptions.GetByUserAsync(userId, ct);
            if (subs.Count > 0)
            {
                await _webPush.SendAsync(subs, title, body, payloadJson, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha no Web Push p/ usuário {UserId}.", userId);
        }
    }
}
