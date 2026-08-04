using MediatR;
using Revoa.Abstractions;
using Revoa.Notifications.Domain.Aggregates.PushSubscriptionAggregate;
using Revoa.Notifications.Domain.Repositories;

namespace Revoa.Notifications.Application.Commands;

// Inscreve (ou atualiza) push subscription do dispositivo. Retorna o id da subscription.
public sealed record SubscribePushCommand(Guid UserId, string Endpoint, string P256dh, string Auth)
    : IRequest<Result<string>>;

public class SubscribePushCommandHandler : IRequestHandler<SubscribePushCommand, Result<string>>
{
    private readonly IPushSubscriptionRepository _subscriptions;

    public SubscribePushCommandHandler(IPushSubscriptionRepository subscriptions)
    {
        _subscriptions = subscriptions;
    }

    public async Task<Result<string>> Handle(SubscribePushCommand request, CancellationToken ct)
    {
        PushSubscription subscription;
        try
        {
            subscription = PushSubscription.Create(request.UserId, request.Endpoint, request.P256dh, request.Auth);
        }
        catch (DomainException ex)
        {
            return Result<string>.Fail(ex.Message);
        }

        await _subscriptions.UpsertAsync(subscription, ct);
        return Result<string>.Ok(subscription.Id.ToString());
    }
}
