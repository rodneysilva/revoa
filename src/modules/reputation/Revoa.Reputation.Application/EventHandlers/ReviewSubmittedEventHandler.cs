using MediatR;
using Microsoft.Extensions.Logging;
using Revoa.IntegrationContracts.Events;
using Revoa.Reputation.Domain.Repositories;
using ReputationAggregate = Revoa.Reputation.Domain.Aggregates.ReputationAggregate;

namespace Revoa.Reputation.Application.EventHandlers;

// Aplica a avaliação pós-troca no score de reputação do avaliado (UF-23): carrega (ou cria) o
// aggregate Reputation do RevieweeId, chama ApplyReview (atualiza ReviewsCount/RatingsSum/média)
// e Upsert. Resiliente: falhas são logadas e NÃO bloqueiam o cadastro da avaliação.
public class ReviewSubmittedEventHandler : INotificationHandler<ReviewSubmittedEvent>
{
    private readonly IReputationRepository _reputation;
    private readonly ILogger<ReviewSubmittedEventHandler> _logger;

    public ReviewSubmittedEventHandler(
        IReputationRepository reputation,
        ILogger<ReviewSubmittedEventHandler> logger)
    {
        _reputation = reputation;
        _logger = logger;
    }

    public async Task Handle(ReviewSubmittedEvent notification, CancellationToken ct)
    {
        try
        {
            var rep = await _reputation.GetByUserIdAsync(notification.RevieweeId, ct)
                      ?? ReputationAggregate.Reputation.Create(notification.RevieweeId);

            rep.ApplyReview(notification.Rating);
            await _reputation.UpsertAsync(rep, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Falha ao aplicar avaliação ao usuário {RevieweeId} (rating {Rating}). Não bloqueia o cadastro.",
                notification.RevieweeId, notification.Rating);
        }
    }
}
