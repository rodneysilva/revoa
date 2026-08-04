namespace Revoa.Catalog.Application.DTOs;

// Item do feed (GET /api/listings/feed) — versão enxuta com distância (se houver raio).
public sealed record FeedItemDto(
    Guid Id,
    string Kind,
    string Modo,
    string Titulo,
    long PrecoRvm,
    string? PrimeiraImagem,
    string VendedorNome,
    string? VendedorAvatarUrl,
    string? Cidade,
    string? Bairro,
    Guid CategoriaId,
    double? DistanciaKm);
