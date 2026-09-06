using MediatR;
using Revoa.Abstractions;
using Revoa.Catalog.Application.DTOs;
using Revoa.Catalog.Domain.Aggregates.ListingAggregate;

namespace Revoa.Catalog.Application.Queries;

// Feed público/anônimo (UF-01). Radius opcional (1/5/10/25 km) aplica Haversine quando lat+lng informados.
// Filtros NxN (todos opcionais, combinam com AND): Modo, PriceMin/Max, DonationOnly, Q (busca textual),
// Sort (recente|preco-asc|preco-desc).
public sealed record GetFeedQuery(
    double? Radius,
    double? Lat,
    double? Lng,
    string? Kind,
    Guid? CategoryId,
    Guid? CommunityId,
    int Page,
    string? Mode,
    long? PriceMin,
    long? PriceMax,
    bool? DonationOnly,
    string? Sort,
    string? Q,
    IReadOnlyList<Guid>? SellerIds) : IRequest<Result<IReadOnlyList<FeedItemDto>>>;
