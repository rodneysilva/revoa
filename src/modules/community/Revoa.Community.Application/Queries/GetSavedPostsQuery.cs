using MediatR;
using Revoa.Abstractions;
using Revoa.Community.Application.DTOs;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Application.Queries;

// Posts salvos pelo usuário (GET /api/posts/saved), na ordem dos bookmarks
// (mais recentes primeiro). Post ocultado/removido depois do bookmark cai fora
// da listagem, mas o bookmark permanece para o toggle não "inverter" estado.
public sealed record GetSavedPostsQuery(Guid UserId)
    : IRequest<Result<IReadOnlyList<PostDto>>>;

public class GetSavedPostsQueryHandler
    : IRequestHandler<GetSavedPostsQuery, Result<IReadOnlyList<PostDto>>>
{
    private readonly ISavedPostRepository _saved;
    private readonly IPostRepository _posts;
    private readonly IPostLikeRepository _likes;

    public GetSavedPostsQueryHandler(
        ISavedPostRepository saved, IPostRepository posts, IPostLikeRepository likes)
    {
        _saved = saved;
        _posts = posts;
        _likes = likes;
    }

    public async Task<Result<IReadOnlyList<PostDto>>> Handle(
        GetSavedPostsQuery request, CancellationToken ct)
    {
        var bookmarks = await _saved.GetByUserAsync(request.UserId, ct);
        if (bookmarks.Count == 0)
        {
            return Result<IReadOnlyList<PostDto>>.Ok(Array.Empty<PostDto>());
        }

        var postsById = (await _posts.GetByIdsAsync(
                bookmarks.Select(b => b.PostId).ToList(), ct))
            .Where(p => p.Status == Domain.Aggregates.PostAggregate.PostStatus.Visible)
            .ToDictionary(p => p.Id);

        // Preserva a ordem dos bookmarks; órfãos caem fora.
        var ordered = bookmarks
            .Select(b => postsById.GetValueOrDefault(b.PostId))
            .Where(p => p is not null)
            .Select(p => p!)
            .ToList();

        var dtos = await PostDtoEnricher.ToDtosAsync(
            ordered, _posts, _likes, _saved, request.UserId, ct,
            forceSavedIds: ordered.Select(p => p.Id).ToList());

        return Result<IReadOnlyList<PostDto>>.Ok(dtos);
    }
}

// Ids de posts salvos (bootstrap do botão 🔖 no FE). Desvia do Result<T> —
// leitura sem falha de negócio (padrão GetLikedPostIdsQuery).
public sealed record GetSavedPostIdsQuery(Guid UserId) : IRequest<IReadOnlyList<Guid>>;

public class GetSavedPostIdsQueryHandler
    : IRequestHandler<GetSavedPostIdsQuery, IReadOnlyList<Guid>>
{
    private readonly ISavedPostRepository _saved;

    public GetSavedPostIdsQueryHandler(ISavedPostRepository saved)
    {
        _saved = saved;
    }

    public async Task<IReadOnlyList<Guid>> Handle(
        GetSavedPostIdsQuery request, CancellationToken ct)
    {
        var bookmarks = await _saved.GetByUserAsync(request.UserId, ct);
        return bookmarks.Select(b => b.PostId).ToList();
    }
}
