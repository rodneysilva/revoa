using Revoa.Catalog.Domain.Aggregates.ListingAggregate;
using Revoa.Catalog.Domain.Repositories;
using Revoa.IntegrationContracts.Pricing;

namespace Revoa.Catalog.Infrastructure.Services;

// Adapter de IListingPriceReader: lê o aggregate Listing (coleção Listings) e devolve amostras de
// preço (ativos com PrecoRvm > 0) + o slug da categoria p/ o módulo Pricing. Mantém o isolamento —
// o Pricing consome só a porta (não acessa a coleção Listings). O slug é resolvido aqui (adapter
// vive no Catalog, que é dono das Categories) para viabilizar o seed BRL por categoria.
public class ListingPriceReader : IListingPriceReader
{
    private readonly IListingRepository _listings;
    private readonly ICategoryRepository _categories;

    public ListingPriceReader(IListingRepository listings, ICategoryRepository categories)
    {
        _listings = listings;
        _categories = categories;
    }

    public async Task<IReadOnlyList<ListingPriceSample>> GetActiveAsync(CancellationToken ct = default)
    {
        var active = await _listings.GetActiveAsync(ct);
        if (active.Count == 0)
        {
            return Array.Empty<ListingPriceSample>();
        }

        // Resolve CategoriaId → Slug uma vez (batch).
        var categories = await _categories.ListActiveAsync(ct);
        var slugByCategory = categories.ToDictionary(c => c.Id, c => c.Slug);

        return active
            .Where(l => l.PrecoRvm > 0)
            .Select(l => new ListingPriceSample(
                l.Id,
                l.PrecoRvm,
                l.CategoriaId,
                l.Kind.ToString(),
                l.Modo.ToString(),
                slugByCategory.TryGetValue(l.CategoriaId, out var slug) ? slug : null))
            .ToList();
    }
}
