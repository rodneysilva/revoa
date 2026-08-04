using MediatR;
using Revoa.Abstractions;
using Revoa.Community.Application.DTOs;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Application.Queries;

// Posts de uma comunidade (anônimo vê — UF-19). ParentId → filhos diretos; senão raízes. Só Visivel.
public sealed record GetCommunityPostsQuery(Guid ComunidadeId, Guid? ParentId, int Page) : IRequest<Result<IReadOnlyList<PostDto>>>;

public class GetCommunityPostsQueryHandler : IRequestHandler<GetCommunityPostsQuery, Result<IReadOnlyList<PostDto>>>
{
    private const int PageSize = 50;

    private readonly IPostRepository _posts;

    public GetCommunityPostsQueryHandler(IPostRepository posts)
    {
        _posts = posts;
    }

    public async Task<Result<IReadOnlyList<PostDto>>> Handle(GetCommunityPostsQuery request, CancellationToken ct)
    {
        var posts = await _posts.GetByComunidadeAsync(request.ComunidadeId, request.ParentId, ct);

        var page = request.Page <= 0 ? 1 : request.Page;
        var paged = posts
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(PostDtoMapper.From)
            .ToList();

        IReadOnlyList<PostDto> result = paged;
        return Result<IReadOnlyList<PostDto>>.Ok(result);
    }
}
