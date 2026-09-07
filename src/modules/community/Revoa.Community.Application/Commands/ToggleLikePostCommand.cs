using MediatR;
using Revoa.Abstractions;
using Revoa.Community.Domain.Aggregates.PostAggregate;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Application.Commands;

// Curtir/descurtir um post (toggle idempotente). Retorna true se agora está
// curtido, false se foi removido. Sem gate de membership: curtir é interação
// leve e o feed público mostra posts de comunidades que o usuário não segue.
// Corrida de insert concorrente cai no índice único ux_Post_User (mesma
// tolerância do catálogo em ToggleSaveListingCommand).
public sealed record ToggleLikePostCommand(Guid PostId, Guid UserId)
    : IRequest<Result<bool>>;

public class ToggleLikePostCommandHandler
    : IRequestHandler<ToggleLikePostCommand, Result<bool>>
{
    private readonly IPostRepository _posts;
    private readonly IPostLikeRepository _likes;

    public ToggleLikePostCommandHandler(IPostRepository posts, IPostLikeRepository likes)
    {
        _posts = posts;
        _likes = likes;
    }

    public async Task<Result<bool>> Handle(ToggleLikePostCommand request, CancellationToken ct)
    {
        var post = await _posts.GetByIdAsync(request.PostId, ct);
        if (post is null || post.Status != PostStatus.Visible)
        {
            return Result<bool>.Fail("Post não encontrado.");
        }

        if (await _likes.ExistsAsync(request.PostId, request.UserId, ct))
        {
            await _likes.RemoveAsync(request.PostId, request.UserId, ct);
            return Result<bool>.Ok(false);
        }

        var like = Revoa.Community.Domain.Aggregates.PostLikeAggregate.PostLike.Create(
            request.PostId, request.UserId);
        await _likes.AddAsync(like, ct);
        return Result<bool>.Ok(true);
    }
}
