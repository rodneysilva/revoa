using MediatR;
using Revoa.Abstractions;
using Revoa.Catalog.Application.DTOs;
using Revoa.Catalog.Domain.Aggregates.ListingAggregate;

namespace Revoa.Catalog.Application.Queries;

// Feed público/anônimo (UF-01). Raio opcional (1/5/10/25 km) aplica Haversine quando lat+lng informados.
// Filtros NxN (todos opcionais, combinam com AND): Modo, PrecoMin/Max, DoarApenas, Q (busca textual),
// Sort (recente|preco-asc|preco-desc).
public sealed record GetFeedQuery(
    double? Raio,
    double? Lat,
    double? Lng,
    string? Kind,
    Guid? CategoriaId,
    Guid? ComunidadeId,
    int Page,
    string? Modo,
    long? PrecoMin,
    long? PrecoMax,
    bool? DoarApenas,
    string? Sort,
    string? Q) : IRequest<Result<IReadOnlyList<FeedItemDto>>>;
