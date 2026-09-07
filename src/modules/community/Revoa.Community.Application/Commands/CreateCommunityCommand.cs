using MediatR;
using Revoa.Abstractions;
using Revoa.Community.Domain.Aggregates.CommunityAggregate;
using Revoa.Community.Domain.Aggregates.MembershipAggregate;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Application.Commands;

// Cria comunidade + vínculo de Criador. Ownership (CreatorId/Nome/Avatar) do token, nunca do body.
public sealed record CreateCommunityCommand(
    Guid CreatorId,
    string CreatorName,
    string? CreatorAvatarUrl,
    string Name,
    string Description,
    CommunityType Type,
    CommunityAxis Axis,
    CommunityVisibility Visibility,
    string? Password,
    double? Lat,
    double? Lng,
    string? Neighborhood,
    string? City,
    string? State,
    string? CoverImageUrl = null) : IRequest<Result<string>>;

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
                request.Name,
                request.Description,
                request.Type,
                request.Axis,
                request.Visibility,
                request.Password,
                request.Lat,
                request.Lng,
                request.Neighborhood,
                request.City,
                request.State,
                request.CreatorId,
                request.CreatorName,
                request.CreatorAvatarUrl,
                request.CoverImageUrl);
        }
        catch (DomainException ex)
        {
            return Result<string>.Fail(ex.Message);
        }

        if (community.Visibility == CommunityVisibility.Private)
        {
            community.SetPasswordHash(PasswordHasher.Hash(request.Password!));
        }

        await _communities.AddAsync(community, ct);

        // Criador vira membro com papel Criador.
        var criador = Membership.Create(
            request.CreatorId,
            request.CreatorName,
            request.CreatorAvatarUrl,
            community.Id,
            MembershipRole.Creator);
        await _memberships.AddAsync(criador, ct);

        return Result<string>.Ok(community.Id.ToString());
    }
}
