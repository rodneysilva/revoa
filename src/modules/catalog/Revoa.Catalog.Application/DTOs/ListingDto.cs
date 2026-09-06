using Revoa.Catalog.Domain.Aggregates.ListingAggregate;

namespace Revoa.Catalog.Application.DTOs;

// Detalhe completo do anúncio (GET /api/listings/{id}).
public sealed record ListingDto(
    Guid Id,
    string Kind,
    string Mode,
    string Title,
    string Description,
    IReadOnlyList<string> Imagens,
    long PriceRvm,
    Guid SellerId,
    string SellerName,
    string? SellerAvatarUrl,
    double? Lat,
    double? Lng,
    string? Neighborhood,
    string? City,
    string? PostalCode,
    Guid CategoryId,
    Guid? CommunityId,
    string Visibility,
    long? NftTokenId,
    string Status,
    ProductDetailsDto? ProductDetails,
    ServiceDetailsDto? ServiceDetails);

public sealed record ProductDetailsDto(string Condition, int Stock);

public sealed record ServiceDetailsDto(string UnitType, int Duration, int VoucherExpiryDays);

public static class ListingDtoMapper
{
    public static ListingDto From(Listing l)
    {
        ProductDetailsDto? product = null;
        if (l.ProductDetails is not null)
        {
            product = new ProductDetailsDto(l.ProductDetails.Condition.ToString(), l.ProductDetails.Stock);
        }

        ServiceDetailsDto? service = null;
        if (l.ServiceDetails is not null)
        {
            service = new ServiceDetailsDto(
                l.ServiceDetails.UnitType.ToString(),
                l.ServiceDetails.Duration,
                l.ServiceDetails.VoucherExpiryDays);
        }

        return new ListingDto(
            l.Id,
            l.Kind.ToString(),
            l.Mode.ToString(),
            l.Title,
            l.Description,
            l.Imagens,
            l.PriceRvm,
            l.SellerId,
            l.SellerName,
            l.SellerAvatarUrl,
            l.Localizacao.Lat,
            l.Localizacao.Lng,
            l.Localizacao.Neighborhood,
            l.Localizacao.City,
            l.Localizacao.PostalCode,
            l.CategoryId,
            l.CommunityId,
            l.Visibility.ToString(),
            l.NftTokenId,
            l.Status.ToString(),
            product,
            service);
    }
}
