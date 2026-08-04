using MediatR;
using Revoa.Abstractions;
using Revoa.Notifications.Domain.Repositories;

namespace Revoa.Notifications.Application.Commands;

// Remove inscrição push (logout/desinstalação/dispositivo inativo). Por Endpoint (chave natural).
public sealed record UnsubscribePushCommand(string Endpoint) : IRequest<Result>;

public class UnsubscribePushCommandHandler : IRequestHandler<UnsubscribePushCommand, Result>
{
    private readonly IPushSubscriptionRepository _subscriptions;

    public UnsubscribePushCommandHandler(IPushSubscriptionRepository subscriptions)
    {
        _subscriptions = subscriptions;
    }

    public async Task<Result> Handle(UnsubscribePushCommand request, CancellationToken ct)
    {
        await _subscriptions.DeleteByEndpointAsync(request.Endpoint, ct);
        return Result.Ok();
    }
}
