using MediatR;
using Revoa.Abstractions;
using Revoa.Catalog.Application.DTOs;
using Revoa.Catalog.Domain.Repositories;

namespace Revoa.Catalog.Application.Queries;

public class GetListingQueryHandler : IRequestHandler<GetListingQuery, Result<ListingDto>>
{
    private readonly IListingRepository _listings;

    public GetListingQueryHandler(IListingRepository listings)
    {
        _listings = listings;
    }

    public async Task<Result<ListingDto>> Handle(GetListingQuery request, CancellationToken ct)
    {
        var listing = await _listings.GetByIdAsync(request.Id, ct);
        if (listing is null)
        {
            return Result<ListingDto>.Fail("Anúncio não encontrado.");
        }

        return Result<ListingDto>.Ok(ListingDtoMapper.From(listing));
    }
}
