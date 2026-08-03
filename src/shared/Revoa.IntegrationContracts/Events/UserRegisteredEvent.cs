namespace Revoa.IntegrationContracts.Events;

public sealed record UserRegisteredEvent(Guid UserId, string Email, string? CouponCode, int Version = 1);
