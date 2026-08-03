using MediatR;
using Revoa.Abstractions;

namespace Revoa.Infrastructure;

// Barramento de eventos de integração in-process (MVP).
// Publica eventos como notificações MediatR quando implementam INotification.
// Extrair para MassTransit quando modularizar para processos separados (ADR-0003).
public class InProcessIntegrationEventBus : IIntegrationEventBus
{
    private readonly IMediator _mediator;

    public InProcessIntegrationEventBus(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default) where T : class
    {
        if (integrationEvent is INotification notification)
        {
            await _mediator.Publish(notification, ct);
        }
    }
}
