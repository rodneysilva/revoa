using MediatR;
using Revoa.Abstractions;
using Revoa.Catalog.Application.DTOs;
using Revoa.Catalog.Application.Services;
using Revoa.Catalog.Domain.Aggregates.ListingAggregate;
using Revoa.Catalog.Domain.Repositories;

namespace Revoa.Catalog.Application.Queries;

public class GetFeedQueryHandler : IRequestHandler<GetFeedQuery, Result<IReadOnlyList<FeedItemDto>>>
{
    private const int PageSize = 24;
    private const int CandidateCap = 200;

    private readonly IListingRepository _listings;

    public GetFeedQueryHandler(IListingRepository listings)
    {
        _listings = listings;
    }

    public async Task<Result<IReadOnlyList<FeedItemDto>>> Handle(GetFeedQuery request, CancellationToken ct)
    {
        ListingKind? kind = null;
        if (!string.IsNullOrWhiteSpace(request.Kind))
        {
            if (!Enum.TryParse<ListingKind>(request.Kind, ignoreCase: true, out var k))
            {
                return Result<IReadOnlyList<FeedItemDto>>.Fail("Kind inválido (Product|Service).");
            }

            kind = k;
        }

        ListingModo? modo = null;
        if (!string.IsNullOrWhiteSpace(request.Modo))
        {
            if (!Enum.TryParse<ListingModo>(request.Modo, ignoreCase: true, out var m))
            {
                return Result<IReadOnlyList<FeedItemDto>>.Fail("Modo inválido (Trocar|Repassar|Doar|Voluntariar).");
            }

            modo = m;
        }

        // Filtro por raio (Haversine) exige lat+lng+raio. Nesse caso a paginação é feita em memória
        // (distância é computada no handler), então o banco devolve só candidatos (cap).
        var useRadius = request.Raio is > 0 && request.Lat is not null && request.Lng is not null;

        var page = request.Page <= 0 ? 1 : request.Page;

        var filter = new FeedFilter(
            kind,
            request.CategoriaId,
            request.ComunidadeId,
            modo,
            request.PrecoMin,
            request.PrecoMax,
            request.DoarApenas,
            request.Q,
            request.Sort,
            page,
            PageSize,
            useRadius,
            CandidateCap,
            request.VendedorIds);

        var candidates = await _listings.GetFeedAsync(filter, ct);

        if (!useRadius)
        {
            IReadOnlyList<FeedItemDto> direct = candidates.Select(Map).ToList();
            return Result<IReadOnlyList<FeedItemDto>>.Ok(direct);
        }

        var lat = request.Lat!.Value;
        var lng = request.Lng!.Value;
        var raio = request.Raio!.Value;

        var withinRadius = candidates
            .Where(l => l.Localizacao.Lat is not null && l.Localizacao.Lng is not null)
            .Select(l => new
            {
                Listing = l,
                Dist = GeoHelper.HaversineKm(lat, lng, l.Localizacao.Lat!.Value, l.Localizacao.Lng!.Value)
            })
            .Where(x => x.Dist <= raio)
            .OrderBy(x => x.Dist)
            .ToList();

        var paged = withinRadius
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(x => Map(x.Listing))
            .ToList();

        return Result<IReadOnlyList<FeedItemDto>>.Ok(paged);
    }

    private static FeedItemDto Map(Listing l) => new(
        l.Id,
        l.Kind.ToString(),
        l.Modo.ToString(),
        l.Titulo,
        l.PrecoRvm,
        l.Imagens.FirstOrDefault(),
        l.VendedorNome,
        l.VendedorAvatarUrl,
        l.Localizacao.Cidade,
        l.Localizacao.Bairro,
        l.CategoriaId,
        DistanciaKm: null,
        Condition: l.ProductDetails?.Condition.ToString(),
        UnitType: l.ServiceDetails?.UnitType.ToString(),
        Duration: l.ServiceDetails?.Duration,
        CreatedAt: l.CreatedAt);
}
