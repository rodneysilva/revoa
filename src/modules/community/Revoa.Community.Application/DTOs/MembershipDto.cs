using Revoa.Community.Domain.Aggregates.MembershipAggregate;

namespace Revoa.Community.Application.DTOs;

public sealed record MembershipDto(
    Guid Id,
    Guid UserId,
    string UserName,
    string? UserAvatarUrl,
    Guid CommunityId,
    MembershipRole Role,
    MembershipStatus Status,
    DateTime JoinedAt);

public static class MembershipDtoMapper
{
    public static MembershipDto From(Membership m) => new(
        m.Id,
        m.UserId,
        m.UserName,
        m.UserAvatarUrl,
        m.CommunityId,
        m.Role,
        m.Status,
        m.JoinedAt);
}
