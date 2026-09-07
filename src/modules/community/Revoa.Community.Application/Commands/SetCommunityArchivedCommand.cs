using MediatR;
using Revoa.Abstractions;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Application.Commands;

// Arquivar/reativar comunidade (painel admin). Arquivada some dos feeds e do
// onboarding (o Default de cidade só auto-vincula se Active); reativar devolve Active.
public sealed record SetCommunityArchivedCommand(Guid CommunityId, bool Archived)
    : IRequest<Result>;

public class SetCommunityArchivedCommandHandler
    : IRequestHandler<SetCommunityArchivedCommand, Result>
{
    private readonly ICommunityRepository _communities;

    public SetCommunityArchivedCommandHandler(ICommunityRepository communities)
    {
        _communities = communities;
    }

    public async Task<Result> Handle(SetCommunityArchivedCommand request, CancellationToken ct)
    {
        var community = await _communities.GetByIdAsync(request.CommunityId, ct);
        if (community is null)
        {
            return Result.Fail("Comunidade não encontrada.");
        }

        if (request.Archived)
        {
            community.Archive();
        }
        else
        {
            try
            {
                community.Reactivate();
            }
            catch (DomainException)
            {
                return Result.Fail("Comunidade não está arquivada.");
            }
        }

        await _communities.UpdateAsync(community, ct);
        return Result.Ok();
    }
}
