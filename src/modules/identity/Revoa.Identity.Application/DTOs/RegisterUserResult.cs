namespace Revoa.Identity.Application.DTOs;

public sealed record RegisterUserResult(
    Guid UserId,
    bool NeedsEmailVerification,
    bool NeedsPhoneVerification);
