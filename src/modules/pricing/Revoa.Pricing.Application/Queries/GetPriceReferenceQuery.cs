using MediatR;
using Revoa.Abstractions;
using Revoa.Pricing.Application.DTOs;
using Revoa.Pricing.Domain.Repositories;

namespace Revoa.Pricing.Application.Queries;

// Consulta a referência de preço justo de UMA categoria (leitura anônima). Retorna null se inexistente.
public sealed record GetPriceReferenceQuery(Guid CategoryId) : IRequest<Result<PriceReferenceDto?>>;

public class GetPriceReferenceQueryHandler : IRequestHandler<GetPriceReferenceQuery, Result<PriceReferenceDto?>>
{
    private readonly IPriceReferenceRepository _repo;

    public GetPriceReferenceQueryHandler(IPriceReferenceRepository repo)
    {
        _repo = repo;
    }

    public async Task<Result<PriceReferenceDto?>> Handle(GetPriceReferenceQuery request, CancellationToken ct)
    {
        var reference = await _repo.GetByCategoryAsync(request.CategoryId, ct);
        return Result<PriceReferenceDto?>.Ok(reference is null ? null : PriceReferenceDtoMapper.From(reference));
    }
}
