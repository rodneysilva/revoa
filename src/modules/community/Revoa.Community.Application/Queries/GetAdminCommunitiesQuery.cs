using MediatR;
using Revoa.Abstractions;
using Revoa.Community.Domain.Aggregates.CommunityAggregate;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Application.Queries;

// Lista de comunidades para o painel admin (TODAS, inclui arquivadas) com
// contagem de membros em batch (mesmo anti-N+1 do feed público).
public sealed record GetAdminCommunitiesQuery : IRequest<Result<IReadOnlyList<AdminCommunityDto>>>;

public sealed record AdminCommunityDto(
    Guid Id,
    string Name,
    string Description,
    CommunityType Type,
    CommunityAxis Axis,
    string? City,
    string? State,
    string CreatorName,
    string Status,
    int MembersCount);

public class GetAdminCommunitiesQueryHandler
    : IRequestHandler<GetAdminCommunitiesQuery, Result<IReadOnlyList<AdminCommunityDto>>>
{
    private readonly ICommunityRepository _communities;
    private readonly IMembershipRepository _memberships;

    public GetAdminCommunitiesQueryHandler(
        ICommunityRepository communities, IMembershipRepository memberships)
    {
        _communities = communities;
        _memberships = memberships;
    }

    public async Task<Result<IReadOnlyList<AdminCommunityDto>>> Handle(
        GetAdminCommunitiesQuery request, CancellationToken ct)
    {
        var communities = await _communities.GetAllAsync(limit: 500, ct);
        if (communities.Count == 0)
        {
            IReadOnlyList<AdminCommunityDto> empty = Array.Empty<AdminCommunityDto>();
            return Result<IReadOnlyList<AdminCommunityDto>>.Ok(empty);
        }

        var counts = await _memberships.CountActiveByCommunityAsync(
            communities.Select(c => c.Id), ct);

        IReadOnlyList<AdminCommunityDto> result = communities
            .Select(c => new AdminCommunityDto(
                c.Id,
                c.Name,
                c.Description,
                c.Type,
                c.Axis,
                c.City,
                c.State,
                c.CreatorName,
                c.Status.ToString(),
                counts.GetValueOrDefault(c.Id)))
            .ToList();

        return Result<IReadOnlyList<AdminCommunityDto>>.Ok(result);
    }
}
