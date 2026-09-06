using MediatR;
using Microsoft.Extensions.Logging;
using Revoa.IntegrationContracts.Events;
using Revoa.IntegrationContracts.Notifications;

namespace Revoa.Notifications.Application.EventHandlers;

// Reage à conclusão de doação/voluntariado (Exchange libera trade total 0) e notifica doador + receptor.
// Resiliente: uma falha de Notify num usuário não bloqueia o outro (try/catch por usuário) nem o publish.
public class DonationCompletedEventHandler : INotificationHandler<DonationCompletedEvent>
{
    private readonly INotifier _notifier;
    private readonly ILogger<DonationCompletedEventHandler> _logger;

    public DonationCompletedEventHandler(INotifier notifier, ILogger<DonationCompletedEventHandler> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Handle(DonationCompletedEvent notification, CancellationToken ct)
    {
        var tradeId = notification.TradeId;
        var payload = $"{{\"tradeId\":\"{tradeId}\",\"modo\":\"{notification.Mode}\"}}";

        // Doador (ofertante): agradecimento + recompensa aplicada.
        await NotifyUserSafe(
            notification.DonorId,
            "Donation",
            "Sua doação foi concluída",
            "Obrigado por ajudar! +recompensa aplicada",
            payload,
            tradeId,
            ct);

        // Receptor: confirmar recebimento.
        await NotifyUserSafe(
            notification.ReceptorId,
            "Donation",
            "Você recebeu uma doação",
            "Confirme o recebimento",
            payload,
            tradeId,
            ct);
    }

    private async Task NotifyUserSafe(
        Guid userId, string type, string title, string body, string payload, Guid tradeId, CancellationToken ct)
    {
        try
        {
            await _notifier.NotifyAsync(userId, type, title, body, payload, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Falha ao notificar usuário {UserId} (doação {TradeId}). Não bloqueia o publish.",
                userId, tradeId);
        }
    }
}
