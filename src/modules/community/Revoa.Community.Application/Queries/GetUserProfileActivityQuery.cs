using MediatR;
using Revoa.Abstractions;
using Revoa.Community.Application.DTOs;
using Revoa.Community.Domain.Aggregates.CommunityAggregate;
using Revoa.Community.Domain.Aggregates.MembershipAggregate;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Application.Queries;

// Atividade pública de um usuário no contexto social (GET /api/users/{id}/communities
// e /api/users/{id}/posts). Comunidades: vínculos Active. Posts: só os de comunidades
// Open — conteúdo de comunidade Private não vaza para visitante do perfil.
public sealed record GetUserCommunitiesQuery(Guid UserId)
    : IRequest<Result<IReadOnlyList<CommunityDto>>>;

public class GetUserCommunitiesQueryHandler
    : IRequestHandler<GetUserCommunitiesQuery, Result<IReadOnlyList<CommunityDto>>>
{
    private readonly ICommunityRepository _communities;
    private readonly IMembershipRepository _memberships;

    public GetUserCommunitiesQueryHandler(
        ICommunityRepository communities,
        IMembershipRepository memberships)
    {
        _communities = communities;
        _memberships = memberships;
    }

    public async Task<Result<IReadOnlyList<CommunityDto>>> Handle(
        GetUserCommunitiesQuery request, CancellationToken ct)
    {
        var memberships = (await _memberships.ListByUsuarioAsync(request.UserId, ct))
            .Where(m => m.Status == MembershipStatus.Active)
            .OrderByDescending(m => m.JoinedAt)
            .ToList();

        if (memberships.Count == 0)
        {
            IReadOnlyList<CommunityDto> empty = Array.Empty<CommunityDto>();
            return Result<IReadOnlyList<CommunityDto>>.Ok(empty);
        }

        var counts = await _memberships.CountActiveByCommunityAsync(
            memberships.Select(m => m.CommunityId), ct);

        var result = new List<CommunityDto>(memberships.Count);
        foreach (var m in memberships)
        {
            var community = await _communities.GetByIdAsync(m.CommunityId, ct);
            if (community is null || community.Status != CommunityStatus.Active)
            {
                continue; // vínculo órfão ou comunidade removida
            }

            result.Add(CommunityDtoMapper.From(community, counts.GetValueOrDefault(community.Id)));
        }

        return Result<IReadOnlyList<CommunityDto>>.Ok(result);
    }
}

// Posts Visivel do autor, mais recentes primeiro, cada um com a comunidade
// (só comunidades Open/Active — mesma regra de exposição do feed público).
public sealed record GetUserPostsQuery(Guid UserId, int Limit = 12)
    : IRequest<Result<IReadOnlyList<SocialFeedItemDto>>>;

public class GetUserPostsQueryHandler
    : IRequestHandler<GetUserPostsQuery, Result<IReadOnlyList<SocialFeedItemDto>>>
{
    private readonly IPostRepository _posts;
    private readonly ICommunityRepository _communities;
    private readonly IMembershipRepository _memberships;

    public GetUserPostsQueryHandler(
        IPostRepository posts,
        ICommunityRepository communities,
        IMembershipRepository memberships)
    {
        _posts = posts;
        _communities = communities;
        _memberships = memberships;
    }

    public async Task<Result<IReadOnlyList<SocialFeedItemDto>>> Handle(
        GetUserPostsQuery request, CancellationToken ct)
    {
        var limit = Math.Clamp(request.Limit, 1, 50);

        var posts = await _posts.GetByAutorAsync(request.UserId, limit, ct);
        if (posts.Count == 0)
        {
            IReadOnlyList<SocialFeedItemDto> empty = Array.Empty<SocialFeedItemDto>();
            return Result<IReadOnlyList<SocialFeedItemDto>>.Ok(empty);
        }

        // Comunidades dos posts: só Open/Active aparecem (Private não vaza).
        var communityIds = posts.Select(p => p.CommunityId).Distinct().ToList();
        var communities = new Dictionary<Guid, CommunityGroup>(communityIds.Count);
        foreach (var id in communityIds)
        {
            var community = await _communities.GetByIdAsync(id, ct);
            if (community is not null
                && community.Status == CommunityStatus.Active
                && community.Visibility == CommunityVisibility.Open)
            {
                communities[id] = community;
            }
        }

        if (communities.Count == 0)
        {
            IReadOnlyList<SocialFeedItemDto> empty = Array.Empty<SocialFeedItemDto>();
            return Result<IReadOnlyList<SocialFeedItemDto>>.Ok(empty);
        }

        var counts = await _memberships.CountActiveByCommunityAsync(communities.Keys, ct);
        var children = await _posts.GetChildrenCountsAsync(
            posts.Select(p => p.Id).ToList(), ct);

        IReadOnlyList<SocialFeedItemDto> result = posts
            .Where(p => communities.ContainsKey(p.CommunityId))
            .Select(p => new SocialFeedItemDto(
                PostDtoMapper.From(p, children.GetValueOrDefault(p.Id)),
                CommunityDtoMapper.From(communities[p.CommunityId], counts.GetValueOrDefault(p.CommunityId))))
            .ToList();

        return Result<IReadOnlyList<SocialFeedItemDto>>.Ok(result);
    }
}
