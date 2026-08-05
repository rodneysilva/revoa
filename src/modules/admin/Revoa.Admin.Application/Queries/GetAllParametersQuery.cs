using MediatR;
using Revoa.Abstractions;
using Revoa.Admin.Application.DTOs;
using Revoa.IntegrationContracts.Admin;

namespace Revoa.Admin.Application.Queries;

// Lista TODOS os parâmetros admin-configuráveis em runtime (UF-30). Monta a lista DEFINIDA de
// chaves conhecidas (catálogo fixo — segurança: só estas chaves são expostas/alteráveis) com
// rótulo/tipo pt-BR e o valor ATUAL lido do IParameterStore. Os defaults abaixo espelham os
// defaults das IOptions/appsettings; com o store vazio o dashboard exibe esses defaults e o
// comportamento do sistema é idêntico ao atual (fallback). Manter em sincronia com os Options.
//
// IMPORTANTE: adicionar um parâmetro aqui exige também adicioná-lo à whitelist do
// SetParameterCommand e consumi-lo no módulo correspondente.
public sealed record GetAllParametersQuery : IRequest<Result<IReadOnlyList<ParameterDto>>>;

public class GetAllParametersQueryHandler
    : IRequestHandler<GetAllParametersQuery, Result<IReadOnlyList<ParameterDto>>>
{
    private readonly IParameterStore _store;

    public GetAllParametersQueryHandler(IParameterStore store)
    {
        _store = store;
    }

    public async Task<Result<IReadOnlyList<ParameterDto>>> Handle(
        GetAllParametersQuery request, CancellationToken ct)
    {
        // Defaults espelham DonationRewardOptions / DemurrageOptions / PricingOptions. Como
        // IParameterStore.GetAsync<T> tem T sem constraint de struct, T? colapsa para o próprio
        // tipo em value types (long/int/bool/decimal) — o valor retornado é sempre concreto
        // (o do store, ou o default passado quando a chave não existe / falha ao desserializar).
        var bonusRvm = await _store.GetAsync("DonationReward.BonusRvm", 2L, ct);
        var repPoints = await _store.GetAsync("DonationReward.ReputationPoints", 10L, ct);
        var helpPoints = await _store.GetAsync("DonationReward.HelpPoints", 1L, ct);

        var monthlyRateBps = await _store.GetAsync("Demurrage.MonthlyRateBps", 50, ct);
        var floorRvm = await _store.GetAsync("Demurrage.FloorRvm", 100L, ct);
        var enabled = await _store.GetAsync("Demurrage.Enabled", true, ct);

        var brlRate = await _store.GetAsync("Pricing.BrlRate", 1.0m, ct);
        var useOllama = await _store.GetAsync("Pricing.UseOllama", true, ct);

        var list = new List<ParameterDto>
        {
            new("DonationReward.BonusRvm", "Bônus de doação (RVM)", "long", bonusRvm),
            new("DonationReward.ReputationPoints", "Pontos de reputação por doação", "long", repPoints),
            new("DonationReward.HelpPoints", "Pontos de ajuda por doação", "long", helpPoints),
            new("Demurrage.MonthlyRateBps", "Demurrage — taxa mensal (bps)", "int", monthlyRateBps),
            new("Demurrage.FloorRvm", "Demurrage — piso de isenção (RVM)", "long", floorRvm),
            new("Demurrage.Enabled", "Demurrage — ativado", "bool", enabled),
            new("Pricing.BrlRate", "Pricing — taxa BRL (1 RVM ≈ R$)", "decimal", brlRate),
            new("Pricing.UseOllama", "Pricing — usar Ollama (LLM local)", "bool", useOllama),
        };

        return Result<IReadOnlyList<ParameterDto>>.Ok(list);
    }
}
