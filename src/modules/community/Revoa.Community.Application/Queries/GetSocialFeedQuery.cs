using MediatR;
using Revoa.Abstractions;
using Revoa.Community.Application.DTOs;
using Revoa.Community.Domain.Aggregates.MembershipAggregate;
using Revoa.Community.Domain.Aggregates.PostAggregate;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Application.Queries;

// Feed social do usuário (GET /api/communities/mine/posts): posts recentes das
// comunidades em que tem vínculo Active — o batimento social do rail "Da sua
// comunidade" no feed, resolvido no servidor em lugar do N+1 que o cliente fazia
// (myCommunities + communityPosts por comunidade + merge no browser).
//
// Inclui raízes e respostas (mesma semântica de "atividade recente"); o rail
// clampa o conteúdo, e resposta é atividade tão social quanto post.
public sealed record GetSocialFeedQuery(Guid UserId, int Limit = 12)
    : IRequest<Result<IReadOnlyList<SocialFeedItemDto>>>;

public sealed record SocialFeedItemDto(PostDto Post, CommunityDto Community);

public class GetSocialFeedQueryHandler
    : IRequestHandler<GetSocialFeedQuery, Result<IReadOnlyList<SocialFeedItemDto>>>
{
    // Vínculos por usuário são poucos; 5 comunidades recentes × limite por
    // comunidade bastam para preencher o rail sem varrer tudo.
    private const int MaxComunidades = 5;

    private readonly IMembershipRepository _memberships;
    private readonly ICommunityRepository _communities;
    private readonly IPostRepository _posts;

    public GetSocialFeedQueryHandler(
        IMembershipRepository memberships,
        ICommunityRepository communities,
        IPostRepository posts)
    {
        _memberships = memberships;
        _communities = communities;
        _posts = posts;
    }

    public async Task<Result<IReadOnlyList<SocialFeedItemDto>>> Handle(
        GetSocialFeedQuery request, CancellationToken ct)
    {
        var limit = Math.Clamp(request.Limit, 1, 50);

        var active = (await _memberships.ListByUsuarioAsync(request.UserId, ct))
            .Where(m => m.Status == MembershipStatus.Active)
            .OrderByDescending(m => m.JoinedAt)
            .Take(MaxComunidades)
            .ToList();

        if (active.Count == 0)
        {
            IReadOnlyList<SocialFeedItemDto> empty = Array.Empty<SocialFeedItemDto>();
            return Result<IReadOnlyList<SocialFeedItemDto>>.Ok(empty);
        }

        var counts = await _memberships.CountActiveByCommunityAsync(
            active.Select(m => m.CommunityId), ct);

        // Comunidade removida = vínculo órfão, pula (mesma regra de GetMyCommunities).
        var items = new List<(Post Post, CommunityDto Community)>();
        foreach (var m in active)
        {
            var community = await _communities.GetByIdAsync(m.CommunityId, ct);
            if (community is null)
            {
                continue;
            }

            var dto = CommunityDtoMapper.From(community, counts.GetValueOrDefault(community.Id));
            var recentes = await _posts.GetByComunidadeRecentAsync(community.Id, limit, ct);
            items.AddRange(recentes.Select(p => (p, dto)));
        }

        // Children counts em batch (anti-N+1, como GetCommunityPostsQuery).
        var maisRecentes = items
            .OrderByDescending(i => i.Post.CreatedAt)
            .Take(limit)
            .ToList();
        var children = await _posts.GetChildrenCountsAsync(
            maisRecentes.Select(i => i.Post.Id).ToList(), ct);

        IReadOnlyList<SocialFeedItemDto> result = maisRecentes
            .Select(i => new SocialFeedItemDto(
                PostDtoMapper.From(i.Post, children.GetValueOrDefault(i.Post.Id)),
                i.Community))
            .ToList();

        return Result<IReadOnlyList<SocialFeedItemDto>>.Ok(result);
    }
}
