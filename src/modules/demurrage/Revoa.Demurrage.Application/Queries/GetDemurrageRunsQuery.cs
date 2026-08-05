using MediatR;
using Revoa.Abstractions;
using Revoa.Demurrage.Application.DTOs;
using Revoa.Demurrage.Domain.Repositories;

namespace Revoa.Demurrage.Application.Queries;

// Histórico de execuções do demurrage (Admin): ordenado por RunAt desc. Limit default 20.
public sealed record GetDemurrageRunsQuery(int Limit = 20) : IRequest<Result<IReadOnlyList<DemurrageRunDto>>>;

public class GetDemurrageRunsQueryHandler
    : IRequestHandler<GetDemurrageRunsQuery, Result<IReadOnlyList<DemurrageRunDto>>>
{
    private readonly IDemurrageRunRepository _repo;

    public GetDemurrageRunsQueryHandler(IDemurrageRunRepository repo)
    {
        _repo = repo;
    }

    public async Task<Result<IReadOnlyList<DemurrageRunDto>>> Handle(
        GetDemurrageRunsQuery request, CancellationToken ct)
    {
        var limit = request.Limit > 0 ? request.Limit : 20;
        var runs = await _repo.ListAsync(limit, ct);
        var dtos = runs.Select(DemurrageRunDtoMapper.From).ToList();
        return Result<IReadOnlyList<DemurrageRunDto>>.Ok(dtos);
    }
}
