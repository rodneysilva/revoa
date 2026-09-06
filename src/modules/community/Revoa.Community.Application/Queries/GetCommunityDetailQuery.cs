using MediatR;
using Revoa.Abstractions;
using Revoa.Community.Application.DTOs;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Application.Queries;

// Detalhe de comunidade (anônimo vê — UF-18). NotFound → Fail.
public sealed record GetCommunityDetailQuery(Guid Id) : IRequest<Result<CommunityDto>>;

public class GetCommunityDetailQueryHandler : IRequestHandler<GetCommunityDetailQuery, Result<CommunityDto>>
{
    private readonly ICommunityRepository _communities;
    private readonly IMembershipRepository _memberships;

    public GetCommunityDetailQueryHandler(ICommunityRepository communities, IMembershipRepository memberships)
    {
        _communities = communities;
        _memberships = memberships;
    }

    public async Task<Result<CommunityDto>> Handle(GetCommunityDetailQuery request, CancellationToken ct)
    {
        var community = await _communities.GetByIdAsync(request.Id, ct);
        if (community is null)
        {
            return Result<CommunityDto>.Fail("Comunidade não encontrada.");
        }

        var counts = await _memberships.CountActiveByCommunityAsync(new[] { community.Id }, ct);
        return Result<CommunityDto>.Ok(CommunityDtoMapper.From(community, counts.GetValueOrDefault(community.Id)));
    }
}
