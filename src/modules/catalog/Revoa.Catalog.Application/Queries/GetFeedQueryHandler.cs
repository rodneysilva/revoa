using MediatR;
using Revoa.Abstractions;
using Revoa.Catalog.Application.DTOs;
using Revoa.Catalog.Application.Services;
using Revoa.Catalog.Domain.Aggregates.ListingAggregate;
using Revoa.Catalog.Domain.Repositories;

namespace Revoa.Catalog.Application.Queries;

public class GetFeedQueryHandler : IRequestHandler<GetFeedQuery, Result<IReadOnlyList<FeedItemDto>>>
{
    private const int PageSize = 20;
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

        var filter = new FeedFilter(kind, request.CategoriaId, request.ComunidadeId, CandidateCap);
        var candidates = await _listings.GetFeedAsync(filter, ct);

        // Filtro por raio (Haversine) quando lat+lng+raio informados.
        var useRadius = request.Raio is > 0 && request.Lat is not null && request.Lng is not null;

        IEnumerable<Listing> stream = candidates;
        if (useRadius)
        {
            var lat = request.Lat!.Value;
            var lng = request.Lng!.Value;
            var raio = request.Raio!.Value;

            stream = candidates
                .Where(l => l.Localizacao.Lat is not null && l.Localizacao.Lng is not null)
                .Select(l => new
                {
                    Listing = l,
                    Dist = GeoHelper.HaversineKm(lat, lng, l.Localizacao.Lat!.Value, l.Localizacao.Lng!.Value)
                })
                .Where(x => x.Dist <= raio)
                .OrderBy(x => x.Dist)
                .Select(x => x.Listing);
        }

        var page = request.Page <= 0 ? 1 : request.Page;
        var paged = stream
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        IReadOnlyList<FeedItemDto> result = paged.Select(Map).ToList();
        return Result<IReadOnlyList<FeedItemDto>>.Ok(result);
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
        DistanciaKm: null);
}
