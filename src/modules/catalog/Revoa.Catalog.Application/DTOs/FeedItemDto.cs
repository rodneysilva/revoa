namespace Revoa.Catalog.Application.DTOs;

// Item do feed (GET /api/listings/feed) — versão enxuta com distância (se houver raio).
public sealed record FeedItemDto(
    Guid Id,
    string Kind,
    string Mode,
    string Title,
    long PriceRvm,
    string? PrimeiraImagem,
    string SellerName,
    string? SellerAvatarUrl,
    string? City,
    string? Neighborhood,
    Guid CategoryId,
    double? DistanciaKm,
    string? Condition,
    string? UnitType,
    int? Duration,
    DateTime CreatedAt,
    string? Description = null);
