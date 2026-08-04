using MediatR;
using Revoa.Abstractions;
using Revoa.Catalog.Domain.Aggregates.CommentAggregate;
using Revoa.Catalog.Domain.Repositories;

namespace Revoa.Catalog.Application.Commands;

// Cria comentário (raiz ou resposta) num anúncio. Autor do token (controller injeta). Depth ≤ 6.
public sealed record CreateCommentCommand(
    Guid AutorId,
    string AutorNome,
    string? AutorAvatarUrl,
    Guid ListingId,
    Guid? ParentId,
    string Conteudo) : IRequest<Result<string>>;

public class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, Result<string>>
{
    private readonly ICommentRepository _comments;

    public CreateCommentCommandHandler(ICommentRepository comments)
    {
        _comments = comments;
    }

    public async Task<Result<string>> Handle(CreateCommentCommand request, CancellationToken ct)
    {
        Comment comment;
        try
        {
            if (request.ParentId is { } parentId)
            {
                var parent = await _comments.GetByIdAsync(parentId, ct);
                if (parent is null || parent.ListingId != request.ListingId)
                {
                    return Result<string>.Fail("Comentário pai não encontrado neste anúncio.");
                }

                comment = Comment.CreateReply(parent, request.AutorId, request.AutorNome, request.AutorAvatarUrl, request.Conteudo);
            }
            else
            {
                comment = Comment.CreateRoot(request.ListingId, request.AutorId, request.AutorNome, request.AutorAvatarUrl, request.Conteudo);
            }
        }
        catch (DomainException ex)
        {
            return Result<string>.Fail(ex.Message);
        }

        await _comments.AddAsync(comment, ct);
        return Result<string>.Ok(comment.Id.ToString());
    }
}
