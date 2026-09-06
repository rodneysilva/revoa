using MediatR;
using Revoa.Abstractions;
using Revoa.Catalog.Domain.Aggregates.ListingAggregate;

namespace Revoa.Catalog.Application.Commands;

// Cria um anúncio. SellerId/Nome/Avatar vêm do usuário autenticado (claim sub + nome),
// injetados pelo controller (ownership nunca vem do body).
public sealed record CreateListingCommand(
    ListingKind Kind,
    ListingMode Mode,
    string Title,
    string Description,
    List<string> Imagens,
    long PriceRvm,
    Guid SellerId,
    string SellerName,
    string? SellerAvatarUrl,
    double? Lat,
    double? Lng,
    string? Neighborhood,
    string? City,
    string? PostalCode,
    Guid CategoryId,
    Guid? CommunityId,
    ListingVisibility Visibility,
    ProductCondition? Condition,
    int? Stock,
    ServiceUnitType? UnitType,
    int? Duration,
    int? VoucherExpiryDays) : IRequest<Result<string>>;
