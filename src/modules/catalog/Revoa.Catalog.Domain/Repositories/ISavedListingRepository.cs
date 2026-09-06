using Revoa.Catalog.Domain.Aggregates.SavedListingAggregate;

namespace Revoa.Catalog.Domain.Repositories;

public interface ISavedListingRepository
{
    Task<bool> ExistsAsync(Guid userId, Guid listingId, CancellationToken ct);

    Task AddAsync(SavedListing saved, CancellationToken ct);

    Task RemoveAsync(Guid userId, Guid listingId, CancellationToken ct);

    // Mais recentes primeiro, cap 200 (bookmark pessoal — sem paginação real).
    Task<IReadOnlyList<SavedListing>> GetByUserAsync(Guid userId, CancellationToken ct);
}
