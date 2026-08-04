using MediatR;
using Revoa.Abstractions;
using Revoa.Exchange.Application.DTOs;
using Revoa.Exchange.Domain.Repositories;

namespace Revoa.Exchange.Application.Queries;

// Fila de doação/voluntariado de um anúncio (GET /api/help?listingId=). Anônimo vê (OOUX 13).
public sealed record GetHelpQueueQuery(Guid ListingId) : IRequest<Result<IReadOnlyList<HelpRequestDto>>>;

public class GetHelpQueueQueryHandler : IRequestHandler<GetHelpQueueQuery, Result<IReadOnlyList<HelpRequestDto>>>
{
    private readonly IHelpRequestRepository _helpRepo;

    public GetHelpQueueQueryHandler(IHelpRequestRepository helpRepo)
    {
        _helpRepo = helpRepo;
    }

    public async Task<Result<IReadOnlyList<HelpRequestDto>>> Handle(GetHelpQueueQuery request, CancellationToken ct)
    {
        var queue = await _helpRepo.GetOpenByListingAsync(request.ListingId, ct);

        IReadOnlyList<HelpRequestDto> result = queue
            .Select(HelpRequestDtoMapper.From)
            .ToList();
        return Result<IReadOnlyList<HelpRequestDto>>.Ok(result);
    }
}
