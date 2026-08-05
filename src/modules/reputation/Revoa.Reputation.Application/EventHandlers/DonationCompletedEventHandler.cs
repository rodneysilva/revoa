using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Revoa.Abstractions;
using Revoa.IntegrationContracts.Admin;
using Revoa.IntegrationContracts.Events;
using Revoa.Reputation.Application.Options;
using Revoa.Reputation.Domain.Repositories;
using ReputationAggregate = Revoa.Reputation.Domain.Aggregates.ReputationAggregate;

namespace Revoa.Reputation.Application.EventHandlers;

// Recompensa multi-eixo do DOADOR ao concluir doação/voluntariado (UF-23): reputação +
// pontos de ajuda + bônus RVM (publica RewardUserEvent p/ o Token mintar via faucet).
// Os valores vêm do IParameterStore (runtime, UF-30) com fallback para os defaults do
// DonationRewardOptions (IOptions/appsettings) — store vazio => comportamento atual.
// Resiliente: falhas são logadas e NÃO bloqueiam o release da doação (event handler isolado).
public class DonationCompletedEventHandler : INotificationHandler<DonationCompletedEvent>
{
    private readonly IReputationRepository _reputation;
    private readonly DonationRewardOptions _options;
    private readonly IParameterStore _parameters;
    private readonly IIntegrationEventBus _bus;
    private readonly ILogger<DonationCompletedEventHandler> _logger;

    public DonationCompletedEventHandler(
        IReputationRepository reputation,
        IOptions<DonationRewardOptions> options,
        IParameterStore parameters,
        IIntegrationEventBus bus,
        ILogger<DonationCompletedEventHandler> logger)
    {
        _reputation = reputation;
        _options = options.Value;
        _parameters = parameters;
        _bus = bus;
        _logger = logger;
    }

    public async Task Handle(DonationCompletedEvent notification, CancellationToken ct)
    {
        try
        {
            var isVolunteer = string.Equals(notification.Modo, "Voluntariar", StringComparison.OrdinalIgnoreCase);

            // Parâmetros runtime com fallback para os defaults do IOptions. GetAsync<T> (T sem
            // constraint) colapsa T? para o próprio tipo em value types -> valor sempre concreto.
            var bonusRvm = await _parameters.GetAsync("DonationReward.BonusRvm", _options.BonusRvm, ct);
            var reputationPoints = await _parameters.GetAsync("DonationReward.ReputationPoints", _options.ReputationPoints, ct);
            var helpPoints = await _parameters.GetAsync("DonationReward.HelpPoints", _options.HelpPoints, ct);

            var rep = await _reputation.GetByUserIdAsync(notification.DonorId, ct)
                      ?? ReputationAggregate.Reputation.Create(notification.DonorId);

            rep.ApplyDonationReward(reputationPoints, helpPoints, isVolunteer);
            await _reputation.UpsertAsync(rep, ct);

            if (bonusRvm > 0)
            {
                await _bus.PublishAsync(
                    new RewardUserEvent(notification.DonorId, bonusRvm, "Bônus de doação"), ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Falha ao aplicar recompensa de doação ao doador {DonorId} (trade {TradeId}). Não bloqueia o release.",
                notification.DonorId, notification.TradeId);
        }
    }
}
