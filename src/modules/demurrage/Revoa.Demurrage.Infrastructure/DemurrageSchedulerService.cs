using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Revoa.Demurrage.Application.Commands;
using Revoa.Demurrage.Application.Options;
using Revoa.Demurrage.Application.Services;
using Revoa.IntegrationContracts.Admin;

namespace Revoa.Demurrage.Infrastructure;

// Scheduler do demurrage (TODO Fase 4 resolvido): roda RunDemurrageCommand no dia 1º às
// 03:00 UTC — sem Quartz, só BackgroundService + Task.Delay até a próxima janela (ver
// DemurrageSchedule). Nos meses de fechamento trimestral (jan/abr/jul/out) aplica antes o
// reajuste IPCA na taxa RUNTIME (IParameterStore), que o admin vê e pode reverter pelo
// painel de parâmetros. Falha do ciclo não mata o serviço: loga e espera o próximo mês.
public class DemurrageSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<DemurrageSchedulerService> _logger;

    public DemurrageSchedulerService(IServiceScopeFactory scopes, ILogger<DemurrageSchedulerService> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = DemurrageSchedule.NextMonthlyRunUtc(DateTimeOffset.UtcNow) - DateTimeOffset.UtcNow;
            try
            {
                await Task.Delay(delay, stoppingToken);
                await RunCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ciclo do scheduler de demurrage falhou — nova tentativa no próximo mês.");
            }
        }
    }

    private async Task RunCycleAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var parameters = scope.ServiceProvider.GetRequiredService<IParameterStore>();
        var ipca = scope.ServiceProvider.GetRequiredService<IIpcaReader>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DemurrageOptions>>().Value;

        // Reajuste trimestral pela inflação: a taxa é o custo de manter RVM ocioso; sem
        // reajuste ela cai em termos reais e o demurrage perde a função.
        if (DemurrageSchedule.IsQuarterStart(DateTime.UtcNow.Month))
        {
            var accumulated = await ipca.GetAccumulatedAsync(3, ct);
            if (accumulated is > 0)
            {
                var current = await parameters.GetAsync(
                    "Demurrage.MonthlyRateBps", options.MonthlyRateBps, ct);
                var adjusted = DemurrageRateAdjuster.Apply(current, accumulated.Value);
                if (adjusted != current)
                {
                    // CT.None: o reajuste vale mesmo se o ciclo cair logo em seguida.
                    await parameters.SetAsync(
                        "Demurrage.MonthlyRateBps", adjusted, "scheduler-ipca", CancellationToken.None);
                    _logger.LogInformation(
                        "Demurrage: reajuste IPCA de {Ipca}% aplicado na taxa — {Current} → {Adjusted} bps.",
                        accumulated.Value, current, adjusted);
                }
            }
        }

        var result = await mediator.Send(new RunDemurrageCommand("scheduler"), ct);
        if (result.IsFailure)
        {
            // Enabled=false é configuração válida (o admin pausou o módulo) — não é erro.
            _logger.LogInformation("Scheduler demurrage: run não executado — {Error}", result.Error);
            return;
        }

        _logger.LogInformation(
            "Scheduler demurrage: run automático concluído — {Affected} carteira(s) afetada(s), {Skipped} isentas/falha.",
            result.Value.AccountsAffected, result.Value.Skipped);
    }
}
