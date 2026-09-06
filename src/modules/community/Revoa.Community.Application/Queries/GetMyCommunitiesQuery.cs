using MediatR;
using Revoa.Abstractions;
using Revoa.Community.Application.DTOs;
using Revoa.Community.Domain.Aggregates.MembershipAggregate;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Application.Queries;

// Comunidades do usuário autenticado (UF-18/UF-19): alimenta "Minhas comunidades"
// e o rail "Da sua comunidade" do feed. Só vínculos Active, mais recente primeiro.
public sealed record GetMyCommunitiesQuery(Guid UserId)
    : IRequest<Result<IReadOnlyList<MyCommunityDto>>>;

// Community serializada + papel/entrada do vínculo (o DTO público não sabe quem pergunta).
public sealed record MyCommunityDto(CommunityDto Community, MembershipRole Role, DateTime JoinedAt);

public class GetMyCommunitiesQueryHandler
    : IRequestHandler<GetMyCommunitiesQuery, Result<IReadOnlyList<MyCommunityDto>>>
{
    private readonly ICommunityRepository _communities;
    private readonly IMembershipRepository _memberships;

    public GetMyCommunitiesQueryHandler(ICommunityRepository communities, IMembershipRepository memberships)
    {
        _communities = communities;
        _memberships = memberships;
    }

    public async Task<Result<IReadOnlyList<MyCommunityDto>>> Handle(
        GetMyCommunitiesQuery request, CancellationToken ct)
    {
        var memberships = await _memberships.ListByUsuarioAsync(request.UserId, ct);
        var active = memberships
            .Where(m => m.Status == MembershipStatus.Active)
            .OrderByDescending(m => m.JoinedAt)
            .ToList();

        if (active.Count == 0)
        {
            IReadOnlyList<MyCommunityDto> empty = Array.Empty<MyCommunityDto>();
            return Result<IReadOnlyList<MyCommunityDto>>.Ok(empty);
        }

        var counts = await _memberships.CountActiveByCommunityAsync(
            active.Select(m => m.CommunityId), ct);

        // Vínculos por usuário são poucos (dezenas no máximo) — o loop evita um
        // método GetManyAsync novo no repositório; comunidade removida = vínculo órfão, pula.
        var result = new List<MyCommunityDto>(active.Count);
        foreach (var m in active)
        {
            var community = await _communities.GetByIdAsync(m.CommunityId, ct);
            if (community is null)
            {
                continue;
            }

            result.Add(new MyCommunityDto(
                CommunityDtoMapper.From(community, counts.GetValueOrDefault(community.Id)),
                m.Role,
                m.JoinedAt));
        }

        return Result<IReadOnlyList<MyCommunityDto>>.Ok(result);
    }
}
