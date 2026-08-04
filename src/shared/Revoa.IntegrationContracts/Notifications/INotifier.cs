namespace Revoa.IntegrationContracts.Notifications;

// Porta (anti-corruption): outros módulos enviam notificações (persistir + in-app + push) sem
// referenciar Notifications.Application. type = nome do enum NotificationType ("Donation",
// "EscrowUpdate", "Offer", ...). payloadJson = JSON p/ deep link (ex.: {"tradeId":"..."}).
public interface INotifier
{
    Task NotifyAsync(
        Guid userId,
        string type,
        string title,
        string body,
        string? payloadJson = null,
        CancellationToken ct = default);
}
