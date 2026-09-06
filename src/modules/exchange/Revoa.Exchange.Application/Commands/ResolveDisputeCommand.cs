using MediatR;
using Revoa.Abstractions;
using Revoa.Exchange.Application.Services;
using Revoa.Exchange.Domain.Aggregates.TradeAggregate;
using Revoa.Exchange.Domain.Repositories;
using Revoa.IntegrationContracts.Events;

namespace Revoa.Exchange.Application.Commands;

// Árbitro/admin resolve disputa: claimArbitrator on-chain (chave dedicada tem ARBITRATOR_ROLE).
// releaseToSeller=true → libera ao vendedor (doação publica DonationCompletedEvent); false → reembolsa.
// Gate: policy "Arbitrator" no endpoint (role ARBITRATOR ou Admin). ResolvedBy = e-mail do árbitro
// (claim do token) para auditoria — espelha DisputeOpenedBy.
public sealed record ResolveDisputeCommand(
    Guid TradeId,
    bool ReleaseToSeller,
    string? ResolvedBy) : IRequest<Result>;

public class ResolveDisputeCommandHandler : IRequestHandler<ResolveDisputeCommand, Result>
{
    private readonly ITradeRepository _tradeRepo;
    private readonly IExchangeEscrowService _escrow;
    private readonly IIntegrationEventBus _eventBus;

    public ResolveDisputeCommandHandler(
        ITradeRepository tradeRepo,
        IExchangeEscrowService escrow,
        IIntegrationEventBus eventBus)
    {
        _tradeRepo = tradeRepo;
        _escrow = escrow;
        _eventBus = eventBus;
    }

    public async Task<Result> Handle(ResolveDisputeCommand request, CancellationToken ct)
    {
        var trade = await _tradeRepo.GetByIdAsync(request.TradeId, ct);
        if (trade is null)
        {
            return Result.Fail("Troca não encontrada.");
        }

        if (trade.State != TradeState.Disputed)
        {
            return Result.Fail("Apenas troca disputada pode ser resolvida por árbitro.");
        }

        if (trade.OnChainTradeId is null)
        {
            return Result.Fail("Troca sem referência on-chain.");
        }

        string txHash;
        try
        {
            txHash = await _escrow.ClaimArbitratorAsync(trade.OnChainTradeId.Value, request.ReleaseToSeller, ct);
        }
        catch (Exception ex)
        {
            return Result.Fail("falha on-chain: " + ex.Message);
        }

        try
        {
            if (request.ReleaseToSeller)
            {
                trade.MarkLiberada(txHash, request.ResolvedBy);
            }
            else
            {
                trade.MarkReembolsada(txHash, request.ResolvedBy);
            }
        }
        catch (DomainException ex)
        {
            return Result.Fail(ex.Message);
        }

        await _tradeRepo.UpdateAsync(trade, ct);

        if (request.ReleaseToSeller && trade.IsDonation)
        {
            await _eventBus.PublishAsync(
                new DonationCompletedEvent(trade.Id, trade.ListingId, trade.SellerId, trade.BuyerId, trade.Mode.ToString()),
                ct);
        }

        return Result.Ok();
    }
}
