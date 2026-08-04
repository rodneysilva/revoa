using Revoa.Community.Domain.Aggregates.MembershipAggregate;

namespace Revoa.Community.Application.DTOs;

public sealed record MembershipDto(
    Guid Id,
    Guid UsuarioId,
    string UsuarioNome,
    string? UsuarioAvatarUrl,
    Guid ComunidadeId,
    MembershipPapel Papel,
    MembershipStatus Status,
    DateTime JoinedAt);

public static class MembershipDtoMapper
{
    public static MembershipDto From(Membership m) => new(
        m.Id,
        m.UsuarioId,
        m.UsuarioNome,
        m.UsuarioAvatarUrl,
        m.ComunidadeId,
        m.Papel,
        m.Status,
        m.JoinedAt);
}
