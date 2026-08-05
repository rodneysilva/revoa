using Revoa.Pricing.Domain.Aggregates.PriceReferenceAggregate;

namespace Revoa.Pricing.Application.DTOs;

// Leitura pública da referência de preço justo de uma categoria (GET /api/pricing/categories/{id}
// e GET /api/pricing — transparência para revoa.org). Todos os campos do aggregate expostos.
public sealed record PriceReferenceDto(
    Guid CategoriaId,
    string? CategoriaSlug,
    long RvmMedian,
    int SampleCount,
    decimal BrlRate,
    long? BrlReference,
    long FairSuggestionRvm,
    decimal? LastIpcRate,
    string? LastIpcMonth,
    string SourcesUsed,
    DateTime UpdatedAt);

public static class PriceReferenceDtoMapper
{
    public static PriceReferenceDto From(PriceReference p) => new(
        p.CategoriaId,
        p.CategoriaSlug,
        p.RvmMedian,
        p.SampleCount,
        p.BrlRate,
        p.BrlReference,
        p.FairSuggestionRvm,
        p.LastIpcRate,
        p.LastIpcMonth,
        p.SourcesUsed,
        p.UpdatedAt);
}
