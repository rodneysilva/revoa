using Revoa.Catalog.Domain.Aggregates.ListingAggregate;

namespace Revoa.Catalog.Application.DTOs;

// Detalhe completo do anúncio (GET /api/listings/{id}).
public sealed record ListingDto(
    Guid Id,
    string Kind,
    string Modo,
    string Titulo,
    string Descricao,
    IReadOnlyList<string> Imagens,
    long PrecoRvm,
    Guid VendedorId,
    string VendedorNome,
    string? VendedorAvatarUrl,
    double? Lat,
    double? Lng,
    string? Bairro,
    string? Cidade,
    string? Cep,
    Guid CategoriaId,
    Guid? ComunidadeId,
    string Visibilidade,
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
            l.Modo.ToString(),
            l.Titulo,
            l.Descricao,
            l.Imagens,
            l.PrecoRvm,
            l.VendedorId,
            l.VendedorNome,
            l.VendedorAvatarUrl,
            l.Localizacao.Lat,
            l.Localizacao.Lng,
            l.Localizacao.Bairro,
            l.Localizacao.Cidade,
            l.Localizacao.Cep,
            l.CategoriaId,
            l.ComunidadeId,
            l.Visibilidade.ToString(),
            l.NftTokenId,
            l.Status.ToString(),
            product,
            service);
    }
}
