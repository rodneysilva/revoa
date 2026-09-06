using Revoa.Abstractions;

namespace Revoa.Catalog.Domain.Aggregates.SavedListingAggregate;

// Anúncio salvo/favoritado pelo usuário (bookmark pessoal — não é público).
// Coleção própria "SavedListings" com índice único (UserId, ListingId): o
// toggle é idempotente por construção.
public class SavedListing : Entity
{
    public Guid UserId { get; private set; }
    public Guid ListingId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private SavedListing() { }

    public static SavedListing Create(Guid userId, Guid listingId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("Usuário é obrigatório.");
        }

        if (listingId == Guid.Empty)
        {
            throw new DomainException("Anúncio é obrigatório.");
        }

        return new SavedListing
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ListingId = listingId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
