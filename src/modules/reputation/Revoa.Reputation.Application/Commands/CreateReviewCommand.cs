using MediatR;
using Revoa.Abstractions;
using Revoa.IntegrationContracts.Events;
using Revoa.IntegrationContracts.Trades;
using Revoa.Reputation.Domain.Aggregates.ReviewAggregate;
using Revoa.Reputation.Domain.Repositories;

namespace Revoa.Reputation.Application.Commands;

// Cria avaliação pós-troca (UF-23): só partes do trade, só trade Liberada, 1 por direção.
// RevieweeId é derivado (contraparte do reviewer) — nunca vem do body. Publica ReviewSubmittedEvent
// p/ o handler aplicar reputation.ApplyReview no avaliado (fecha o loop do rating).
public sealed record CreateReviewCommand(
    Guid ReviewerId,
    string ReviewerName,
    Guid TradeId,
    int Rating,
    string? Comment) : IRequest<Result<string>>;

public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, Result<string>>
{
    private readonly ITradeInfoProvider _trades;
    private readonly IReviewRepository _reviews;
    private readonly IIntegrationEventBus _bus;

    public CreateReviewCommandHandler(
        ITradeInfoProvider trades,
        IReviewRepository reviews,
        IIntegrationEventBus bus)
    {
        _trades = trades;
        _reviews = reviews;
        _bus = bus;
    }

    public async Task<Result<string>> Handle(CreateReviewCommand request, CancellationToken ct)
    {
        try
        {
            var trade = await _trades.GetByIdAsync(request.TradeId, ct);
            if (trade is null)
            {
                return Result<string>.Fail("Troca não encontrada.");
            }

            if (!string.Equals(trade.State, "Released", StringComparison.OrdinalIgnoreCase))
            {
                return Result<string>.Fail("Só é possível avaliar trocas concluídas (liberadas).");
            }

            if (request.ReviewerId != trade.SellerId && request.ReviewerId != trade.BuyerId)
            {
                return Result<string>.Fail("Só as partes da troca podem avaliar.");
            }

            var revieweeId = request.ReviewerId == trade.SellerId ? trade.BuyerId : trade.SellerId;

            var existing = await _reviews.GetByTradeAndReviewerAsync(request.TradeId, request.ReviewerId, ct);
            if (existing is not null)
            {
                return Result<string>.Fail("Você já avaliou esta troca.");
            }

            var review = Review.Create(
                request.TradeId,
                request.ReviewerId,
                request.ReviewerName,
                revieweeId,
                request.Rating,
                request.Comment);

            await _reviews.AddAsync(review, ct);

            await _bus.PublishAsync(new ReviewSubmittedEvent(revieweeId, request.Rating), ct);

            return Result<string>.Ok(review.Id.ToString());
        }
        catch (DomainException ex)
        {
            return Result<string>.Fail(ex.Message);
        }
    }
}
