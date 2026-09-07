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

        ListingMode? modo = null;
        if (!string.IsNullOrWhiteSpace(request.Mode))
        {
            if (!Enum.TryParse<ListingMode>(request.Mode, ignoreCase: true, out var m))
            {
                return Result<IReadOnlyList<FeedItemDto>>.Fail("Modo inválido (Trade|Resell|Donate|Volunteer).");
            }

            modo = m;
        }

        // Filtro por raio (Haversine) exige lat+lng+raio. Nesse caso a paginação é feita em memória
        // (distância é computada no handler), então o banco devolve só candidatos (cap).
        var useRadius = request.Radius is > 0 && request.Lat is not null && request.Lng is not null;

        var page = request.Page <= 0 ? 1 : request.Page;

        var filter = new FeedFilter(
            kind,
            request.CategoryId,
            request.CommunityId,
            modo,
            request.PriceMin,
            request.PriceMax,
            request.DonationOnly,
            request.Q,
            request.Sort,
            page,
            PageSize,
            useRadius,
            CandidateCap,
            request.SellerIds);

        var candidates = await _listings.GetFeedAsync(filter, ct);

        if (!useRadius)
        {
            IReadOnlyList<FeedItemDto> direct = candidates.Select(Map).ToList();
            return Result<IReadOnlyList<FeedItemDto>>.Ok(direct);
        }

        var lat = request.Lat!.Value;
        var lng = request.Lng!.Value;
        var raio = request.Radius!.Value;

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
        l.Mode.ToString(),
        l.Title,
        l.PriceRvm,
        l.Imagens.FirstOrDefault(),
        l.SellerName,
        l.SellerAvatarUrl,
        l.Localizacao.City,
        l.Localizacao.Neighborhood,
        l.CategoryId,
        DistanciaKm: null,
        Condition: l.ProductDetails?.Condition.ToString(),
        UnitType: l.ServiceDetails?.UnitType.ToString(),
        Duration: l.ServiceDetails?.Duration,
        CreatedAt: l.CreatedAt,
        Description: l.Description);
}
