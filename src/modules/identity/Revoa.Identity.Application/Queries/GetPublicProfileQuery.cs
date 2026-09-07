using MediatR;
using Revoa.Abstractions;
using Revoa.Identity.Domain.Aggregates.UserAggregate;
using Revoa.Identity.Domain.Repositories;

namespace Revoa.Identity.Application.Queries;

// Perfil público (GET /api/users/{id}): só o que é público — nome, "membro
// desde" e selo de verificação (email+telefone). E-mail/telefone NUNCA saem.
// Banido/inativo = 404.
public sealed record GetPublicProfileQuery(Guid UserId)
    : IRequest<Result<PublicProfileDto>>;

public sealed record PublicProfileDto(
    Guid Id,
    string Name,
    DateTime? MemberSince,
    bool Verified = false);

public class GetPublicProfileQueryHandler
    : IRequestHandler<GetPublicProfileQuery, Result<PublicProfileDto>>
{
    private readonly IUserRepository _users;

    public GetPublicProfileQueryHandler(IUserRepository users)
    {
        _users = users;
    }

    public async Task<Result<PublicProfileDto>> Handle(GetPublicProfileQuery request, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(request.UserId, ct);
        if (user is null || user.Status is UserStatus.Banned or UserStatus.Inactive)
        {
            return Result<PublicProfileDto>.Fail("Usuário não encontrado.");
        }

        // Documentos anteriores ao campo CreatedAt ficam com default → null (não exibir).
        var memberSince = user.CreatedAt == default ? (DateTime?)null : user.CreatedAt;

        return Result<PublicProfileDto>.Ok(new PublicProfileDto(
            user.Id, user.Name, memberSince, user.EmailVerified && user.PhoneVerified));
    }
}
