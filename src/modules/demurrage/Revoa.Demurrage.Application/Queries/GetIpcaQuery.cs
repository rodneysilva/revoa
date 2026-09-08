using MediatR;
using Microsoft.Extensions.Options;
using Revoa.Abstractions;
using Revoa.Demurrage.Application.Options;
using Revoa.Demurrage.Application.Services;
using Revoa.IntegrationContracts.Admin;

namespace Revoa.Demurrage.Application.Queries;

public sealed record IpcaStatusDto(
    double? AccumulatedQuarterPercent,
    int CurrentRateBps,
    int? AdjustedRateBps,
    DateTime NextRunUtc);

public sealed record GetIpcaQuery : IRequest<Result<IpcaStatusDto>>;

// Situação do reajuste IPCA para o admin (GET /api/demurrage/ipca): acumulado do último
// trimestre, taxa atual (runtime) e a taxa ajustada que o scheduler aplicaria no próximo
// fechamento trimestral. IPCA indisponível → Accumulated/Adjusted null (a taxa segue).
public class GetIpcaQueryHandler : IRequestHandler<GetIpcaQuery, Result<IpcaStatusDto>>
{
    private readonly IIpcaReader _ipca;
    private readonly IParameterStore _parameters;
    private readonly DemurrageOptions _options;

    public GetIpcaQueryHandler(
        IIpcaReader ipca,
        IParameterStore parameters,
        IOptions<DemurrageOptions> options)
    {
        _ipca = ipca;
        _parameters = parameters;
        _options = options.Value;
    }

    public async Task<Result<IpcaStatusDto>> Handle(GetIpcaQuery request, CancellationToken ct)
    {
        var current = await _parameters.GetAsync("Demurrage.MonthlyRateBps", _options.MonthlyRateBps, ct);
        var accumulated = await _ipca.GetAccumulatedAsync(3, ct);

        return Result<IpcaStatusDto>.Ok(new IpcaStatusDto(
            accumulated,
            current,
            accumulated is null ? null : DemurrageRateAdjuster.Apply(current, accumulated.Value),
            DemurrageSchedule.NextMonthlyRunUtc(DateTimeOffset.UtcNow).UtcDateTime));
    }
}
