using Revoa.Catalog.Domain.Aggregates.ListingLikeAggregate;

namespace Revoa.Catalog.Domain.Repositories;

public interface IListingLikeRepository
{
    Task<bool> ExistsAsync(Guid listingId, Guid userId, CancellationToken ct);

    Task AddAsync(ListingLike like, CancellationToken ct);

    Task RemoveAsync(Guid listingId, Guid userId, CancellationToken ct);

    // Todos os ListingIds curtidos pelo usuário (cap 500 — bootstrap do botão
    // curtir no FE, mesma pegada do saved/ids).
    Task<IReadOnlyList<Guid>> GetAllLikedIdsAsync(Guid userId, CancellationToken ct);
}
