using MediatR;
using Revoa.Abstractions;
using Revoa.Community.Domain.Aggregates.MembershipAggregate;
using Revoa.Community.Domain.Aggregates.PostAggregate;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Application.Commands;

// Cria post (raiz ou resposta). Exige autor membro ativo. Resposta valida depth ≤ 6.
public sealed record CreatePostCommand(
    Guid AutorId,
    string AutorNome,
    string? AutorAvatarUrl,
    Guid ComunidadeId,
    Guid? ParentId,
    string Conteudo) : IRequest<Result<string>>;

public class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, Result<string>>
{
    private readonly IPostRepository _posts;
    private readonly IMembershipRepository _memberships;

    public CreatePostCommandHandler(IPostRepository posts, IMembershipRepository memberships)
    {
        _posts = posts;
        _memberships = memberships;
    }

    public async Task<Result<string>> Handle(CreatePostCommand request, CancellationToken ct)
    {
        // Gate: autor deve ser membro ativo da comunidade.
        var membership = await _memberships.GetByUsuarioEComunidadeAsync(request.AutorId, request.ComunidadeId, ct);
        if (membership is null || membership.Status != MembershipStatus.Ativa)
        {
            return Result<string>.Fail("Apenas membros ativos podem postar nesta comunidade.");
        }

        Post post;
        try
        {
            if (request.ParentId is null)
            {
                post = Post.CreateRoot(
                    request.ComunidadeId,
                    request.AutorId,
                    request.AutorNome,
                    request.AutorAvatarUrl,
                    request.Conteudo);
            }
            else
            {
                var parent = await _posts.GetByIdAsync(request.ParentId.Value, ct);
                if (parent is null || parent.ComunidadeId != request.ComunidadeId)
                {
                    return Result<string>.Fail("Post pai não encontrado nesta comunidade.");
                }

                post = Post.CreateReply(
                    parent,
                    request.AutorId,
                    request.AutorNome,
                    request.AutorAvatarUrl,
                    request.Conteudo);
            }
        }
        catch (DomainException ex)
        {
            return Result<string>.Fail(ex.Message);
        }

        await _posts.AddAsync(post, ct);
        return Result<string>.Ok(post.Id.ToString());
    }
}
