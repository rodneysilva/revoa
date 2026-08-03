namespace Revoa.Abstractions;

public interface IIntegrationEventBus
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default) where T : class;
}
