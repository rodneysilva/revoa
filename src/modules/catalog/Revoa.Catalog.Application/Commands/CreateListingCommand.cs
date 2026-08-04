using MediatR;
using Revoa.Abstractions;
using Revoa.Catalog.Domain.Aggregates.ListingAggregate;

namespace Revoa.Catalog.Application.Commands;

// Cria um anúncio. VendedorId/Nome/Avatar vêm do usuário autenticado (claim sub + nome),
// injetados pelo controller (ownership nunca vem do body).
public sealed record CreateListingCommand(
    ListingKind Kind,
    ListingModo Modo,
    string Titulo,
    string Descricao,
    List<string> Imagens,
    long PrecoRvm,
    Guid VendedorId,
    string VendedorNome,
    string? VendedorAvatarUrl,
    double? Lat,
    double? Lng,
    string? Bairro,
    string? Cidade,
    string? Cep,
    Guid CategoriaId,
    Guid? ComunidadeId,
    ListingVisibilidade Visibilidade,
    ProductCondition? Condition,
    int? Stock,
    ServiceUnitType? UnitType,
    int? Duration,
    int? VoucherExpiryDays) : IRequest<Result<string>>;
