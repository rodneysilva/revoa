using MediatR;
using Revoa.Abstractions;
using Revoa.Catalog.Domain.Aggregates.ListingAggregate;
using Revoa.Catalog.Domain.Repositories;

namespace Revoa.Catalog.Application.Commands;

// Curtir/descurtir um anúncio (toggle idempotente). Retorna true se agora está
// curtido, false se foi removido. Sem gate de membership: o card de anúncio na
// comunidade se comporta como um post e curtir é interação leve. Corrida de
// insert concorrente cai no índice único ux_Listing_User.
public sealed record ToggleLikeListingCommand(Guid ListingId, Guid UserId)
    : IRequest<Result<bool>>;

public class ToggleLikeListingCommandHandler
    : IRequestHandler<ToggleLikeListingCommand, Result<bool>>
{
    private readonly IListingRepository _listings;
    private readonly IListingLikeRepository _likes;

    public ToggleLikeListingCommandHandler(
        IListingRepository listings,
        IListingLikeRepository likes)
    {
        _listings = listings;
        _likes = likes;
    }

    public async Task<Result<bool>> Handle(ToggleLikeListingCommand request, CancellationToken ct)
    {
        var listing = await _listings.GetByIdAsync(request.ListingId, ct);
        if (listing is null || listing.Status != ListingStatus.Active)
        {
            return Result<bool>.Fail("Anúncio não encontrado.");
        }

        if (await _likes.ExistsAsync(request.ListingId, request.UserId, ct))
        {
            await _likes.RemoveAsync(request.ListingId, request.UserId, ct);
            return Result<bool>.Ok(false);
        }

        var like = Revoa.Catalog.Domain.Aggregates.ListingLikeAggregate.ListingLike.Create(
            request.ListingId, request.UserId);
        await _likes.AddAsync(like, ct);
        return Result<bool>.Ok(true);
    }
}
