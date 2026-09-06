using MediatR;
using Revoa.Abstractions;
using Revoa.Catalog.Domain.Aggregates.ListingAggregate;
using Revoa.Catalog.Domain.Repositories;

namespace Revoa.Catalog.Application.Commands;

// Salvar/dessalvar um anúncio (bookmark pessoal, idempotente). Retorna true se
// agora está salvo, false se foi removido. Só anúncio ativo pode ser salvo.
public sealed record ToggleSaveListingCommand(Guid UserId, Guid ListingId)
    : IRequest<Result<bool>>;

public class ToggleSaveListingCommandHandler
    : IRequestHandler<ToggleSaveListingCommand, Result<bool>>
{
    private readonly IListingRepository _listings;
    private readonly ISavedListingRepository _saved;

    public ToggleSaveListingCommandHandler(
        IListingRepository listings,
        ISavedListingRepository saved)
    {
        _listings = listings;
        _saved = saved;
    }

    public async Task<Result<bool>> Handle(ToggleSaveListingCommand request, CancellationToken ct)
    {
        var listing = await _listings.GetByIdAsync(request.ListingId, ct);
        if (listing is null || listing.Status != ListingStatus.Active)
        {
            return Result<bool>.Fail("Anúncio não encontrado.");
        }

        if (await _saved.ExistsAsync(request.UserId, request.ListingId, ct))
        {
            await _saved.RemoveAsync(request.UserId, request.ListingId, ct);
            return Result<bool>.Ok(false);
        }

        var bookmark = Revoa.Catalog.Domain.Aggregates.SavedListingAggregate.SavedListing.Create(
            request.UserId, request.ListingId);
        await _saved.AddAsync(bookmark, ct);
        return Result<bool>.Ok(true);
    }
}
