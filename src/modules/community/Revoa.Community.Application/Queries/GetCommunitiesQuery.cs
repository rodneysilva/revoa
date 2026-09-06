using MediatR;
using Revoa.Abstractions;
using Revoa.Community.Application.DTOs;
using Revoa.Community.Application.Services;
using Revoa.Community.Domain.Aggregates.CommunityAggregate;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Application.Queries;

// Feed de comunidades públicas (UF-18). Radius opcional aplica Haversine. Ordena por mais recente (Version desc).
public sealed record GetCommunitiesQuery(
    double? Radius,
    double? Lat,
    double? Lng,
    CommunityAxis? Axis,
    int Page) : IRequest<Result<IReadOnlyList<CommunityDto>>>;

public class GetCommunitiesQueryHandler : IRequestHandler<GetCommunitiesQuery, Result<IReadOnlyList<CommunityDto>>>
{
    private const int PageSize = 20;
    private const int CandidateCap = 200;

    private readonly ICommunityRepository _communities;
    private readonly IMembershipRepository _memberships;

    public GetCommunitiesQueryHandler(ICommunityRepository communities, IMembershipRepository memberships)
    {
        _communities = communities;
        _memberships = memberships;
    }

    public async Task<Result<IReadOnlyList<CommunityDto>>> Handle(GetCommunitiesQuery request, CancellationToken ct)
    {
        var candidates = await _communities.GetPublicAsync(
            new PublicFilter(request.Axis, null, CandidateCap), ct);

        var useRadius = request.Radius is > 0 && request.Lat is not null && request.Lng is not null;

        IEnumerable<CommunityGroup> stream = candidates;
        if (useRadius)
        {
            var lat = request.Lat!.Value;
            var lng = request.Lng!.Value;
            var raio = request.Radius!.Value;

            stream = candidates
                .Where(c => c.Lat is not null && c.Lng is not null)
                .Select(c => new
                {
                    Community = c,
                    Dist = Geo.DistanceKm(lat, lng, c.Lat!.Value, c.Lng!.Value)
                })
                .Where(x => x.Dist <= raio)
                .OrderBy(x => x.Dist)
                .Select(x => x.Community);
        }

        var page = request.Page <= 0 ? 1 : request.Page;
        var paged = stream
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        if (paged.Count == 0)
        {
            IReadOnlyList<CommunityDto> empty = Array.Empty<CommunityDto>();
            return Result<IReadOnlyList<CommunityDto>>.Ok(empty);
        }

        var counts = await _memberships.CountActiveByCommunityAsync(paged.Select(c => c.Id), ct);

        IReadOnlyList<CommunityDto> result = paged
            .Select(c => CommunityDtoMapper.From(c, counts.GetValueOrDefault(c.Id)))
            .ToList();
        return Result<IReadOnlyList<CommunityDto>>.Ok(result);
    }
}
