using MediatR;
using Revoa.Abstractions;
using Revoa.Community.Application.DTOs;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Application.Queries;

// Posts de uma comunidade (anônimo vê — UF-19). ParentId → filhos diretos; senão raízes. Só Visivel.
// ViewerId opcional popula IsLiked/IsSaved quando há JWT (endpoint AllowAnonymous).
public sealed record GetCommunityPostsQuery(Guid CommunityId, Guid? ParentId, int Page, Guid? ViewerId = null)
    : IRequest<Result<IReadOnlyList<PostDto>>>;

public class GetCommunityPostsQueryHandler : IRequestHandler<GetCommunityPostsQuery, Result<IReadOnlyList<PostDto>>>
{
    private const int PageSize = 50;

    private readonly IPostRepository _posts;
    private readonly IPostLikeRepository _likes;
    private readonly ISavedPostRepository _saved;

    public GetCommunityPostsQueryHandler(
        IPostRepository posts, IPostLikeRepository likes, ISavedPostRepository saved)
    {
        _posts = posts;
        _likes = likes;
        _saved = saved;
    }

    public async Task<Result<IReadOnlyList<PostDto>>> Handle(GetCommunityPostsQuery request, CancellationToken ct)
    {
        var posts = await _posts.GetByCommunityAsync(request.CommunityId, request.ParentId, ct);

        var page = request.Page <= 0 ? 1 : request.Page;
        var paged = posts
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        var result = await PostDtoEnricher.ToDtosAsync(paged, _posts, _likes, _saved, request.ViewerId, ct);
        return Result<IReadOnlyList<PostDto>>.Ok(result);
    }
}
