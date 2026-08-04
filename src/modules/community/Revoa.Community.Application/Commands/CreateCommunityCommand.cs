using MediatR;
using Revoa.Abstractions;
using Revoa.Community.Domain.Aggregates.CommunityAggregate;
using Revoa.Community.Domain.Aggregates.MembershipAggregate;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Application.Commands;

// Cria comunidade + vínculo de Criador. Ownership (CriadorId/Nome/Avatar) do token, nunca do body.
public sealed record CreateCommunityCommand(
    Guid CriadorId,
    string CriadorNome,
    string? CriadorAvatarUrl,
    string Nome,
    string Descricao,
    CommunityTipo Tipo,
    CommunityEixo Eixo,
    CommunityVisibilidade Visibilidade,
    string? Password,
    double? Lat,
    double? Lng,
    string? Bairro,
    string? Cidade,
    string? Estado) : IRequest<Result<string>>;

public class CreateCommunityCommandHandler : IRequestHandler<CreateCommunityCommand, Result<string>>
{
    private readonly ICommunityRepository _communities;
    private readonly IMembershipRepository _memberships;

    public CreateCommunityCommandHandler(ICommunityRepository communities, IMembershipRepository memberships)
    {
        _communities = communities;
        _memberships = memberships;
    }

    public async Task<Result<string>> Handle(CreateCommunityCommand request, CancellationToken ct)
    {
        CommunityGroup community;
        try
        {
            community = CommunityGroup.Create(
                request.Nome,
                request.Descricao,
                request.Tipo,
                request.Eixo,
                request.Visibilidade,
                request.Password,
                request.Lat,
                request.Lng,
                request.Bairro,
                request.Cidade,
                request.Estado,
                request.CriadorId,
                request.CriadorNome,
                request.CriadorAvatarUrl);
        }
        catch (DomainException ex)
        {
            return Result<string>.Fail(ex.Message);
        }

        if (community.Visibilidade == CommunityVisibilidade.Private)
        {
            community.SetPasswordHash(PasswordHasher.Hash(request.Password!));
        }

        await _communities.AddAsync(community, ct);

        // Criador vira membro com papel Criador.
        var criador = Membership.Create(
            request.CriadorId,
            request.CriadorNome,
            request.CriadorAvatarUrl,
            community.Id,
            MembershipPapel.Criador);
        await _memberships.AddAsync(criador, ct);

        return Result<string>.Ok(community.Id.ToString());
    }
}
