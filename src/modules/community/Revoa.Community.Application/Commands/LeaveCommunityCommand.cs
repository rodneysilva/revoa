using MediatR;
using Revoa.Abstractions;
using Revoa.Community.Domain.Aggregates.MembershipAggregate;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Application.Commands;

// Sai de comunidade (remove o membership). Criador deve arquivar a comunidade em vez de sair.
public sealed record LeaveCommunityCommand(Guid UserId, Guid CommunityId) : IRequest<Result>;

public class LeaveCommunityCommandHandler : IRequestHandler<LeaveCommunityCommand, Result>
{
    private readonly IMembershipRepository _memberships;

    public LeaveCommunityCommandHandler(IMembershipRepository memberships)
    {
        _memberships = memberships;
    }

    public async Task<Result> Handle(LeaveCommunityCommand request, CancellationToken ct)
    {
        var membership = await _memberships.GetByUserAndCommunityAsync(request.UserId, request.CommunityId, ct);
        if (membership is null)
        {
            return Result.Fail("Você não é membro desta comunidade.");
        }

        if (membership.Role == MembershipRole.Creator)
        {
            return Result.Fail("Criador deve arquivar a comunidade em vez de sair.");
        }

        await _memberships.DeleteAsync(membership, ct);
        return Result.Ok();
    }
}
