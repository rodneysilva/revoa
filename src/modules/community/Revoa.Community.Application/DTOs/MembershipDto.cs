using Revoa.Community.Domain.Aggregates.MembershipAggregate;

namespace Revoa.Community.Application.DTOs;

public sealed record MembershipDto(
    Guid Id,
    Guid UsuarioId,
    string UserName,
    string? UsuarioAvatarUrl,
    Guid CommunityId,
    MembershipRole Role,
    MembershipStatus Status,
    DateTime JoinedAt);

public static class MembershipDtoMapper
{
    public static MembershipDto From(Membership m) => new(
        m.Id,
        m.UsuarioId,
        m.UserName,
        m.UsuarioAvatarUrl,
        m.CommunityId,
        m.Role,
        m.Status,
        m.JoinedAt);
}
