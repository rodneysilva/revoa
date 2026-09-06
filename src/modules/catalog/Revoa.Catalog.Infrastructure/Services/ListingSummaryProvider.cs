using Revoa.Catalog.Domain.Aggregates.ListingAggregate;
using Revoa.Catalog.Domain.Repositories;
using Revoa.IntegrationContracts.Listings;

namespace Revoa.Catalog.Infrastructure.Services;

// Adapter de IListingSummaryProvider: lê o aggregate Listing (coleção Listings) e devolve um
// resumo imutável p/ o módulo Exchange. Mantém o isolamento — o Exchange consome só o port.
public class ListingSummaryProvider : IListingSummaryProvider
{
    private readonly IListingRepository _listings;

    public ListingSummaryProvider(IListingRepository listings)
    {
        _listings = listings;
    }

    public async Task<ListingSummary?> GetByIdAsync(Guid listingId, CancellationToken ct = default)
    {
        var listing = await _listings.GetByIdAsync(listingId, ct);
        if (listing is null)
        {
            return null;
        }

        var voucherExpiryDays = listing.ServiceDetails?.VoucherExpiryDays ?? 30;
        var modo = listing.Mode.ToString();

        return new ListingSummary(
            listing.Id,
            listing.Kind.ToString(),
            modo,
            listing.PriceRvm,
            listing.SellerId,
            listing.SellerName,
            listing.SellerAvatarUrl,
            listing.NftTokenId,
            voucherExpiryDays,
            listing.Status.ToString(),
            IsDonation(modo));
    }

    private static bool IsDonation(string modo)
        => string.Equals(modo, nameof(ListingMode.Donate), StringComparison.OrdinalIgnoreCase)
           || string.Equals(modo, nameof(ListingMode.Volunteer), StringComparison.OrdinalIgnoreCase);
}
