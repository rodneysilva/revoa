using MediatR;
using Revoa.Abstractions;
using Revoa.Community.Domain.Aggregates.MembershipAggregate;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Application.Commands;

// Define/remove a capa da comunidade (upload via /api/media?folder=communities).
// Gate: Criador ou Moderador com vínculo Active. CoverImageUrl null volta ao gradiente.
public sealed record SetCommunityCoverCommand(Guid CommunityId, Guid UserId, string? CoverImageUrl)
    : IRequest<Result>;

public class SetCommunityCoverCommandHandler
    : IRequestHandler<SetCommunityCoverCommand, Result>
{
    private readonly ICommunityRepository _communities;
    private readonly IMembershipRepository _memberships;

    public SetCommunityCoverCommandHandler(
        ICommunityRepository communities, IMembershipRepository memberships)
    {
        _communities = communities;
        _memberships = memberships;
    }

    public async Task<Result> Handle(SetCommunityCoverCommand request, CancellationToken ct)
    {
        var community = await _communities.GetByIdAsync(request.CommunityId, ct);
        if (community is null)
        {
            return Result.Fail("Comunidade não encontrada.");
        }

        // Gate: criador ou moderador da própria comunidade.
        var membership = await _memberships.GetByUserAndCommunityAsync(
            request.UserId, request.CommunityId, ct);
        if (membership is null
            || membership.Status != MembershipStatus.Active
            || membership.Role is not (MembershipRole.Creator or MembershipRole.Moderator))
        {
            return Result.Fail("Apenas moderadores ou o criador podem alterar a capa.");
        }

        try
        {
            community.SetCoverImageUrl(request.CoverImageUrl);
        }
        catch (DomainException ex)
        {
            return Result.Fail(ex.Message);
        }

        await _communities.UpdateAsync(community, ct);

        return Result.Ok();
    }
}
