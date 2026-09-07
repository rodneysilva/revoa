using Revoa.Abstractions;

namespace Revoa.Community.Domain.Aggregates.SavedPostAggregate;

// Post salvo/favoritado pelo usuário (bookmark pessoal — não é público).
// Coleção própria "SavedPosts" com índice único (UserId, PostId): o toggle é
// idempotente por construção. Guarda CommunityId para leitura sem join.
public class SavedPost : Entity
{
    public Guid UserId { get; private set; }
    public Guid PostId { get; private set; }
    public Guid CommunityId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private SavedPost() { }

    public static SavedPost Create(Guid userId, Guid postId, Guid communityId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("Usuário é obrigatório.");
        }

        if (postId == Guid.Empty)
        {
            throw new DomainException("Post é obrigatório.");
        }

        if (communityId == Guid.Empty)
        {
            throw new DomainException("Comunidade é obrigatória.");
        }

        return new SavedPost
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PostId = postId,
            CommunityId = communityId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
