using MediatR;
using Revoa.Abstractions;
using Revoa.Exchange.Domain.Aggregates.HelpRequestAggregate;
using Revoa.Exchange.Domain.Repositories;
using Revoa.IntegrationContracts.Listings;

namespace Revoa.Exchange.Application.Commands;

// Pedido de ajuda (entra na fila de doação/voluntariado — OOUX 13). Anônimo NÃO pode; gate Verified.
public sealed record RequestHelpCommand(
    Guid AuthorId,
    string AuthorName,
    string? AuthorAvatarUrl,
    Guid ListingId,
    string Message) : IRequest<Result<string>>;

public class RequestHelpCommandHandler : IRequestHandler<RequestHelpCommand, Result<string>>
{
    private readonly IListingSummaryProvider _listingProvider;
    private readonly IHelpRequestRepository _helpRepo;

    public RequestHelpCommandHandler(
        IListingSummaryProvider listingProvider,
        IHelpRequestRepository helpRepo)
    {
        _listingProvider = listingProvider;
        _helpRepo = helpRepo;
    }

    public async Task<Result<string>> Handle(RequestHelpCommand request, CancellationToken ct)
    {
        var listing = await _listingProvider.GetByIdAsync(request.ListingId, ct);
        if (listing is null)
        {
            return Result<string>.Fail("Anúncio não encontrado.");
        }

        if (!listing.IsDonation)
        {
            return Result<string>.Fail("Apenas anúncios de doação/voluntariado aceitam pedidos de ajuda.");
        }

        HelpRequest help;
        try
        {
            help = HelpRequest.Create(
                request.ListingId,
                request.AuthorId,
                request.AuthorName,
                request.AuthorAvatarUrl,
                request.Message);
        }
        catch (DomainException ex)
        {
            return Result<string>.Fail(ex.Message);
        }

        await _helpRepo.AddAsync(help, ct);

        return Result<string>.Ok(help.Id.ToString());
    }
}
