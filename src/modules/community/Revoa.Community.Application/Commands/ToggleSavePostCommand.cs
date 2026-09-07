using MediatR;
using Revoa.Abstractions;
using Revoa.Community.Domain.Aggregates.PostAggregate;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Application.Commands;

// Salvar/dessalvar um post (bookmark pessoal, idempotente). Retorna true se
// agora está salvo, false se foi removido. Só post Visível pode ser salvo.
public sealed record ToggleSavePostCommand(Guid PostId, Guid UserId)
    : IRequest<Result<bool>>;

public class ToggleSavePostCommandHandler
    : IRequestHandler<ToggleSavePostCommand, Result<bool>>
{
    private readonly IPostRepository _posts;
    private readonly ISavedPostRepository _saved;

    public ToggleSavePostCommandHandler(IPostRepository posts, ISavedPostRepository saved)
    {
        _posts = posts;
        _saved = saved;
    }

    public async Task<Result<bool>> Handle(ToggleSavePostCommand request, CancellationToken ct)
    {
        var post = await _posts.GetByIdAsync(request.PostId, ct);
        if (post is null || post.Status != PostStatus.Visible)
        {
            return Result<bool>.Fail("Post não encontrado.");
        }

        if (await _saved.ExistsAsync(request.UserId, request.PostId, ct))
        {
            await _saved.RemoveAsync(request.UserId, request.PostId, ct);
            return Result<bool>.Ok(false);
        }

        var bookmark = Revoa.Community.Domain.Aggregates.SavedPostAggregate.SavedPost.Create(
            request.UserId, request.PostId, post.CommunityId);
        await _saved.AddAsync(bookmark, ct);
        return Result<bool>.Ok(true);
    }
}
