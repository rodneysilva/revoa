using MediatR;
using Revoa.Abstractions;
using Revoa.Catalog.Application.DTOs;
using Revoa.Catalog.Domain.Aggregates.ListingAggregate;
using Revoa.Catalog.Domain.Repositories;

namespace Revoa.Catalog.Application.Queries;

// Anúncios salvos pelo usuário (lista "Salvos" do perfil). O bookmark guarda
// apenas ids — aqui faz o join com listings ativos e devolve cards prontos
// (mesmo DTO do feed). Privado: sempre do usuário do token.
public sealed record GetSavedListingsQuery(Guid UserId)
    : IRequest<Result<IReadOnlyList<FeedItemDto>>>;

public class GetSavedListingsQueryHandler
    : IRequestHandler<GetSavedListingsQuery, Result<IReadOnlyList<FeedItemDto>>>
{
    private readonly ISavedListingRepository _saved;
    private readonly IListingRepository _listings;

    public GetSavedListingsQueryHandler(
        ISavedListingRepository saved,
        IListingRepository listings)
    {
        _saved = saved;
        _listings = listings;
    }

    public async Task<Result<IReadOnlyList<FeedItemDto>>> Handle(
        GetSavedListingsQuery request, CancellationToken ct)
    {
        var bookmarks = await _saved.GetByUserAsync(request.UserId, ct);
        if (bookmarks.Count == 0)
        {
            return Result<IReadOnlyList<FeedItemDto>>.Ok(
                Array.Empty<FeedItemDto>());
        }

        var ids = bookmarks.Select(b => b.ListingId).ToList();
        var listings = await _listings.GetByIdsAsync(ids, ct);

        // Preserva a ordem dos bookmarks (mais recentes primeiro).
        var porId = listings.ToDictionary(l => l.Id);
        IReadOnlyList<FeedItemDto> result = ids
            .Where(porId.ContainsKey)
            .Select(id => Map(porId[id]))
            .ToList();

        return Result<IReadOnlyList<FeedItemDto>>.Ok(result);
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

// Ids dos anúncios salvos (bootstrap do estado do botão salvar no FE — uma
// chamada, sem N+1 por card). Desvio do Result<T>: lista não tem modo de
// falha de negócio (em erro de banco a exceção sobe, como no unread-count).
public sealed record GetSavedListingIdsQuery(Guid UserId)
    : IRequest<IReadOnlyList<Guid>>;

public class GetSavedListingIdsQueryHandler
    : IRequestHandler<GetSavedListingIdsQuery, IReadOnlyList<Guid>>
{
    private readonly ISavedListingRepository _saved;

    public GetSavedListingIdsQueryHandler(ISavedListingRepository saved)
    {
        _saved = saved;
    }

    public async Task<IReadOnlyList<Guid>> Handle(
        GetSavedListingIdsQuery request, CancellationToken ct)
    {
        var bookmarks = await _saved.GetByUserAsync(request.UserId, ct);
        return bookmarks.Select(b => b.ListingId).ToList();
    }
}
