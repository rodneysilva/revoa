using MediatR;
using Revoa.Abstractions;
using Revoa.Notifications.Domain.Repositories;

namespace Revoa.Notifications.Application.Commands;

// Marca notificação como lida. Ownership: só o dono (UserId == notif.UserId) pode marcar.
public sealed record MarkNotificationReadCommand(Guid UserId, Guid NotificationId) : IRequest<Result>;

public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, Result>
{
    private readonly INotificationRepository _notifications;

    public MarkNotificationReadCommandHandler(INotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        var notification = await _notifications.GetByIdAsync(request.NotificationId, ct);
        if (notification is null)
        {
            return Result.Fail("Notificação não encontrada.");
        }

        // Segurança: não revela existência a não-donos — mesma mensagem neutra.
        if (notification.UserId != request.UserId)
        {
            return Result.Fail("Notificação não encontrada.");
        }

        try
        {
            notification.MarkRead();
        }
        catch (DomainException ex)
        {
            return Result.Fail(ex.Message);
        }

        await _notifications.UpdateAsync(notification, ct);
        return Result.Ok();
    }
}
