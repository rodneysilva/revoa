using MediatR;
using Revoa.Abstractions;
using Revoa.Community.Application.DTOs;
using Revoa.Community.Domain.Aggregates.CommunityAggregate;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Application.Queries;

// Feed público de posts (GET /api/posts): raízes Visible das comunidades
// Active, mais recentes primeiro, 20 por página. Anônimo vê (paridade da
// leitura pública atual — só Archived sai). Reposição limitada: busca o dobro
// da página e descarta posts de comunidades arquivadas/removidas.
public sealed record GetPublicPostsQuery(int Page = 1, Guid? ViewerId = null)
    : IRequest<Result<IReadOnlyList<PublicPostItemDto>>>;

public sealed record PublicPostItemDto(PostDto Post, string CommunityName, string? CommunityCoverUrl = null);

public class GetPublicPostsQueryHandler
    : IRequestHandler<GetPublicPostsQuery, Result<IReadOnlyList<PublicPostItemDto>>>
{
    private const int PageSize = 20;

    private readonly IPostRepository _posts;
    private readonly ICommunityRepository _communities;
    private readonly IPostLikeRepository _likes;
    private readonly ISavedPostRepository _saved;

    public GetPublicPostsQueryHandler(
        IPostRepository posts,
        ICommunityRepository communities,
        IPostLikeRepository likes,
        ISavedPostRepository saved)
    {
        _posts = posts;
        _communities = communities;
        _likes = likes;
        _saved = saved;
    }

    public async Task<Result<IReadOnlyList<PublicPostItemDto>>> Handle(
        GetPublicPostsQuery request, CancellationToken ct)
    {
        var page = Math.Clamp(request.Page, 1, 100);
        var skip = (page - 1) * PageSize;

        // Dobro da página para repor posts descartados (comunidade arquivada/removida).
        var fetched = await _posts.GetRecentRootsAsync(skip, PageSize * 2, ct);
        if (fetched.Count == 0)
        {
            return Result<IReadOnlyList<PublicPostItemDto>>.Ok(Array.Empty<PublicPostItemDto>());
        }

        var communitiesById = (await _communities.GetByIdsAsync(
                fetched.Select(p => p.CommunityId).Distinct().ToList(), ct))
            .ToDictionary(c => c.Id);

        var pagePosts = fetched
            .Where(p => communitiesById.TryGetValue(p.CommunityId, out var c)
                        && c.Status == CommunityStatus.Active)
            .Take(PageSize)
            .ToList();

        var dtos = await PostDtoEnricher.ToDtosAsync(
            pagePosts, _posts, _likes, _saved, request.ViewerId, ct);

        IReadOnlyList<PublicPostItemDto> result = dtos
            .Select(d => new PublicPostItemDto(
                d,
                communitiesById[d.CommunityId].Name,
                communitiesById[d.CommunityId].CoverImageUrl))
            .ToList();

        return Result<IReadOnlyList<PublicPostItemDto>>.Ok(result);
    }
}
