using MediatR;
using Revoa.Abstractions;
using Revoa.Community.Domain.Aggregates.CommunityAggregate;
using Revoa.Community.Domain.Aggregates.MembershipAggregate;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Application.Commands;

// Entra em comunidade. Private valida senha. Não permite reentrar se já ativo ou bloqueado.
public sealed record JoinCommunityCommand(
    Guid UsuarioId,
    string UsuarioNome,
    string? UsuarioAvatarUrl,
    Guid ComunidadeId,
    string? Password) : IRequest<Result<string>>;

public class JoinCommunityCommandHandler : IRequestHandler<JoinCommunityCommand, Result<string>>
{
    private readonly ICommunityRepository _communities;
    private readonly IMembershipRepository _memberships;

    public JoinCommunityCommandHandler(ICommunityRepository communities, IMembershipRepository memberships)
    {
        _communities = communities;
        _memberships = memberships;
    }

    public async Task<Result<string>> Handle(JoinCommunityCommand request, CancellationToken ct)
    {
        var community = await _communities.GetByIdAsync(request.ComunidadeId, ct);
        if (community is null)
        {
            return Result<string>.Fail("Comunidade não encontrada.");
        }

        if (community.Status == CommunityStatus.Archived)
        {
            return Result<string>.Fail("Comunidade arquivada.");
        }

        if (community.Visibilidade == CommunityVisibilidade.Private
            && !PasswordHasher.Verify(request.Password ?? string.Empty, community.PasswordHash))
        {
            return Result<string>.Fail("Senha incorreta.");
        }

        // Upgrade transparente: senha legada (SHA256+salt estático) confirmada → rehash PBKDF2.
        if (community.Visibilidade == CommunityVisibilidade.Private
            && PasswordHasher.IsLegacy(community.PasswordHash))
        {
            community.SetPasswordHash(PasswordHasher.Hash(request.Password!));
            await _communities.UpdateAsync(community, ct);
        }

        var existing = await _memberships.GetByUsuarioEComunidadeAsync(request.UsuarioId, request.ComunidadeId, ct);
        if (existing is not null)
        {
            return Result<string>.Fail(existing.Status == MembershipStatus.Bloqueada
                ? "Você está bloqueado desta comunidade."
                : "Você já é membro desta comunidade.");
        }

        var membership = Membership.Create(
            request.UsuarioId,
            request.UsuarioNome,
            request.UsuarioAvatarUrl,
            request.ComunidadeId,
            MembershipPapel.Membro);
        await _memberships.AddAsync(membership, ct);

        return Result<string>.Ok(request.ComunidadeId.ToString());
    }
}
