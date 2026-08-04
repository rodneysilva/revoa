using Revoa.Catalog.Domain.Aggregates.ListingAggregate;

namespace Revoa.Catalog.Domain.Repositories;

// Filtro de feed aplicado no MongoDB (antes do Haversine em memória, na camada de aplicação).
public record FeedFilter(
    ListingKind? Kind,
    Guid? CategoriaId,
    Guid? ComunidadeId,
    int Limit);

public interface IListingRepository
{
    Task<Listing?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<Listing>> GetFeedAsync(FeedFilter filter, CancellationToken ct);

    Task AddAsync(Listing listing, CancellationToken ct);

    Task UpdateAsync(Listing listing, CancellationToken ct);
}
