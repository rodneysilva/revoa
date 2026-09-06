using MediatR;
using Revoa.Abstractions;
using Revoa.Community.Domain.Aggregates.MembershipAggregate;
using Revoa.Community.Domain.Aggregates.PostAggregate;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Application.Commands;

// Cria post (raiz ou resposta). Exige autor membro ativo. Resposta valida depth ≤ 6.
public sealed record CreatePostCommand(
    Guid AutorId,
    string AuthorName,
    string? AutorAvatarUrl,
    Guid CommunityId,
    Guid? ParentId,
    string Content) : IRequest<Result<string>>;

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
        var membership = await _memberships.GetByUserAndCommunityAsync(request.AutorId, request.CommunityId, ct);
        if (membership is null || membership.Status != MembershipStatus.Active)
        {
            return Result<string>.Fail("Apenas membros ativos podem postar nesta comunidade.");
        }

        Post post;
        try
        {
            if (request.ParentId is null)
            {
                post = Post.CreateRoot(
                    request.CommunityId,
                    request.AutorId,
                    request.AuthorName,
                    request.AutorAvatarUrl,
                    request.Content);
            }
            else
            {
                var parent = await _posts.GetByIdAsync(request.ParentId.Value, ct);
                if (parent is null || parent.CommunityId != request.CommunityId)
                {
                    return Result<string>.Fail("Post pai não encontrado nesta comunidade.");
                }

                post = Post.CreateReply(
                    parent,
                    request.AutorId,
                    request.AuthorName,
                    request.AutorAvatarUrl,
                    request.Content);
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
