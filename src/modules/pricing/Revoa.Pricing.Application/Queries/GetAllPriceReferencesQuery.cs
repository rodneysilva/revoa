using MediatR;
using Revoa.Abstractions;
using Revoa.Pricing.Application.DTOs;
using Revoa.Pricing.Domain.Repositories;

namespace Revoa.Pricing.Application.Queries;

// Lista todas as referências de preço justo (leitura anônima — transparência; alimenta revoa.org).
// Ordenado por UpdatedAt desc.
public sealed record GetAllPriceReferencesQuery : IRequest<Result<IReadOnlyList<PriceReferenceDto>>>;

public class GetAllPriceReferencesQueryHandler
    : IRequestHandler<GetAllPriceReferencesQuery, Result<IReadOnlyList<PriceReferenceDto>>>
{
    private readonly IPriceReferenceRepository _repo;

    public GetAllPriceReferencesQueryHandler(IPriceReferenceRepository repo)
    {
        _repo = repo;
    }

    public async Task<Result<IReadOnlyList<PriceReferenceDto>>> Handle(
        GetAllPriceReferencesQuery request, CancellationToken ct)
    {
        var references = await _repo.GetAllAsync(ct);
        var dtos = references.Select(PriceReferenceDtoMapper.From).ToList();
        return Result<IReadOnlyList<PriceReferenceDto>>.Ok(dtos);
    }
}
