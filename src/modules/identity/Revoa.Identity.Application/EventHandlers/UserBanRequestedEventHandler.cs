using MediatR;
using Microsoft.Extensions.Logging;
using Revoa.Identity.Domain.Repositories;
using Revoa.IntegrationContracts.Events;

namespace Revoa.Identity.Application.EventHandlers;

// Consome o pedido de banimento vindo do módulo Moderation (UF-25): carrega o usuário, chama
// User.Ban() (Status=Banned) e persiste. Resiliente: falhas são logadas e NÃO bloqueiam a
// resolução da denúncia — o administrador pode reprocessar. Mantém o Identity como único
// mutador do agregado User (o Moderation não referencia IUserRepository).
public class UserBanRequestedEventHandler : INotificationHandler<UserBanRequestedEvent>
{
    private readonly IUserRepository _users;
    private readonly ILogger<UserBanRequestedEventHandler> _logger;

    public UserBanRequestedEventHandler(
        IUserRepository users,
        ILogger<UserBanRequestedEventHandler> logger)
    {
        _users = users;
        _logger = logger;
    }

    public async Task Handle(UserBanRequestedEvent notification, CancellationToken ct)
    {
        try
        {
            var user = await _users.GetByIdAsync(notification.UserId, ct);
            if (user is null)
            {
                _logger.LogWarning(
                    "UserBanRequested: usuário {UserId} não encontrado. Banimento ignorado.",
                    notification.UserId);
                return;
            }

            user.Ban();
            await _users.UpdateAsync(user, ct);

            _logger.LogInformation(
                "Usuário {UserId} banido por moderação: {Reason}.",
                notification.UserId, notification.Reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Falha ao banir usuário {UserId} ({Reason}). Não bloqueia a resolução da denúncia.",
                notification.UserId, notification.Reason);
        }
    }
}
