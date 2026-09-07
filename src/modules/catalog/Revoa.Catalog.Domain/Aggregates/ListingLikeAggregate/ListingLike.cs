using Revoa.Abstractions;

namespace Revoa.Catalog.Domain.Aggregates.ListingLikeAggregate;

// Curtida em anúncio (interação pública com contador). Coleção própria
// "ListingLikes" com índice único (ListingId, UserId): toggle idempotente —
// mesmo motivo do PostLike: embutir no Listing bateria com o optimistic lock
// do UpdateAsync em curtidas concorrentes.
public class ListingLike : Entity
{
    public Guid ListingId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ListingLike() { }

    public static ListingLike Create(Guid listingId, Guid userId)
    {
        if (listingId == Guid.Empty)
        {
            throw new DomainException("Anúncio é obrigatório.");
        }

        if (userId == Guid.Empty)
        {
            throw new DomainException("Usuário é obrigatório.");
        }

        return new ListingLike
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
