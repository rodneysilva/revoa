using MediatR;

namespace Revoa.IntegrationContracts.Events;

public sealed record WalletCreatedEvent(Guid UserId, string WalletAddress, int Version = 1)
    : INotification;
