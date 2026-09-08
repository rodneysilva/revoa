using Revoa.Catalog.Domain.Aggregates.ListingAggregate;

namespace Revoa.Catalog.Domain.Repositories;

// Filtro NxN de feed aplicado no MongoDB (AND). RadiusMode=true pula a paginação
// no banco (Haversine é feita em memória no handler) e usa CandidateCap.
public record FeedFilter(
    ListingKind? Kind,
    Guid? CategoryId,
    Guid? CommunityId,
    ListingMode? Mode,
    long? PriceMin,
    long? PriceMax,
    bool? DonationOnly,
    string? Q,
    string? Sort,
    int Page,
    int PageSize,
    bool RadiusMode,
    int CandidateCap,
    IReadOnlyList<Guid>? SellerIds,
    bool OnlyCommunity = false);

public interface IListingRepository
{
    Task<Listing?> GetByIdAsync(Guid id, CancellationToken ct);

    // Vários por id, só ativos (usado pela lista de Salvos — join com bookmark).
    Task<IReadOnlyList<Listing>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct);

    Task<IReadOnlyList<Listing>> GetFeedAsync(FeedFilter filter, CancellationToken ct);

    // Todos os anúncios ativos (sem paginação/geo). Usado pelo adapter de Pricing (mediana comunitária).
    Task<IReadOnlyList<Listing>> GetActiveAsync(CancellationToken ct);

    // Produtos ativos ainda sem NFT (mint falhou na criação ou anúncio seedado direto no banco).
    // Alimenta o reconciliador de mint em lotes pequenos, mais antigos primeiro.
    Task<IReadOnlyList<Listing>> GetUnmintedProductsAsync(int limit, CancellationToken ct);

    Task AddAsync(Listing listing, CancellationToken ct);

    Task UpdateAsync(Listing listing, CancellationToken ct);
}
