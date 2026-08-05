using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Revoa.Abstractions;
using Revoa.IntegrationContracts.Events;
using Revoa.Reputation.Application.Options;
using Revoa.Reputation.Domain.Repositories;
using ReputationAggregate = Revoa.Reputation.Domain.Aggregates.ReputationAggregate;

namespace Revoa.Reputation.Application.EventHandlers;

// Recompensa multi-eixo do DOADOR ao concluir doação/voluntariado (UF-23): reputação +
// pontos de ajuda + bônus RVM (publica RewardUserEvent p/ o Token mintar via faucet).
// Resiliente: falhas são logadas e NÃO bloqueiam o release da doação (event handler isolado).
public class DonationCompletedEventHandler : INotificationHandler<DonationCompletedEvent>
{
    private readonly IReputationRepository _reputation;
    private readonly DonationRewardOptions _options;
    private readonly IIntegrationEventBus _bus;
    private readonly ILogger<DonationCompletedEventHandler> _logger;

    public DonationCompletedEventHandler(
        IReputationRepository reputation,
        IOptions<DonationRewardOptions> options,
        IIntegrationEventBus bus,
        ILogger<DonationCompletedEventHandler> logger)
    {
        _reputation = reputation;
        _options = options.Value;
        _bus = bus;
        _logger = logger;
    }

    public async Task Handle(DonationCompletedEvent notification, CancellationToken ct)
    {
        try
        {
            var isVolunteer = string.Equals(notification.Modo, "Voluntariar", StringComparison.OrdinalIgnoreCase);

            var rep = await _reputation.GetByUserIdAsync(notification.DonorId, ct)
                      ?? ReputationAggregate.Reputation.Create(notification.DonorId);

            rep.ApplyDonationReward(_options.ReputationPoints, _options.HelpPoints, isVolunteer);
            await _reputation.UpsertAsync(rep, ct);

            if (_options.BonusRvm > 0)
            {
                await _bus.PublishAsync(
                    new RewardUserEvent(notification.DonorId, _options.BonusRvm, "Bônus de doação"), ct);
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
