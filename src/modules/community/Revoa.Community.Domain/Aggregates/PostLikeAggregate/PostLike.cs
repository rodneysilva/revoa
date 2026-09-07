using Revoa.Abstractions;

namespace Revoa.Community.Domain.Aggregates.PostLikeAggregate;

// Curtida em post (pública — alimenta o contador). Coleção própria "PostLikes"
// com índice único (PostId, UserId): o toggle é idempotente por construção e o
// like NÃO reescreve o documento do Post (que tem optimistic lock por Version).
public class PostLike : Entity
{
    public Guid PostId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PostLike() { }

    public static PostLike Create(Guid postId, Guid userId)
    {
        if (postId == Guid.Empty)
        {
            throw new DomainException("Post é obrigatório.");
        }

        if (userId == Guid.Empty)
        {
            throw new DomainException("Usuário é obrigatório.");
        }

        return new PostLike
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
