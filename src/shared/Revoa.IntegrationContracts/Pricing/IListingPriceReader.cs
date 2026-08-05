namespace Revoa.IntegrationContracts.Pricing;

// Porta (anti-corruption): permite que o módulo Pricing leia amostras de preços de anúncios ativos
// SEM acessar a coleção Listings (isolamento de módulos). O adapter vive em Revoa.Catalog.Infrastructure
// (lê o aggregate Listing + resolve o slug da categoria). Tipos primitivos para não acoplar enums do Catalog.
// Mediana de PrecoRvm por CategoriaId = base estatística comunitária da referência de preço justo.
public sealed record ListingPriceSample(
    Guid ListingId,
    long PrecoRvm,
    Guid CategoriaId,
    string Kind,                 // "Product" | "Service"
    string Modo,                 // "Trocar" | "Repassar" | "Doar" | "Voluntariar"
    string? CategoriaSlug);      // slug da categoria (resolve seed BRL por categoria; null se desconhecido)

public interface IListingPriceReader
{
    // Anúncios ativos com PrecoRvm > 0 (doar/voluntariar = 0 RVM são excluídos).
    Task<IReadOnlyList<ListingPriceSample>> GetActiveAsync(CancellationToken ct = default);
}
