using MediatR;
using Revoa.Abstractions;
using Revoa.Exchange.Application.Services;
using Revoa.Exchange.Domain.Aggregates.TradeAggregate;
using Revoa.Exchange.Domain.Repositories;
using Revoa.IntegrationContracts.Events;
using Revoa.IntegrationContracts.UserWallets;

namespace Revoa.Exchange.Application.Commands;

// Liberação cooperativa (seller OU buyer) — release on-chain. Doação publica DonationCompletedEvent.
public sealed record ReleaseTradeCommand(
    Guid ActorId,
    Guid TradeId) : IRequest<Result>;

public class ReleaseTradeCommandHandler : IRequestHandler<ReleaseTradeCommand, Result>
{
    private readonly ITradeRepository _tradeRepo;
    private readonly IUserWalletProvider _walletProvider;
    private readonly IExchangeEscrowService _escrow;
    private readonly IIntegrationEventBus _eventBus;

    public ReleaseTradeCommandHandler(
        ITradeRepository tradeRepo,
        IUserWalletProvider walletProvider,
        IExchangeEscrowService escrow,
        IIntegrationEventBus eventBus)
    {
        _tradeRepo = tradeRepo;
        _walletProvider = walletProvider;
        _escrow = escrow;
        _eventBus = eventBus;
    }

    public async Task<Result> Handle(ReleaseTradeCommand request, CancellationToken ct)
    {
        var trade = await _tradeRepo.GetByIdAsync(request.TradeId, ct);
        if (trade is null)
        {
            return Result.Fail("Troca não encontrada.");
        }

        if (trade.State is not (TradeState.Funded or TradeState.Disputed))
        {
            return Result.Fail("Troca não está em estado de liberação.");
        }

        var isSeller = trade.SellerId == request.ActorId;
        var isBuyer = trade.BuyerId == request.ActorId;
        if (!isSeller && !isBuyer)
        {
            return Result.Fail("Apenas vendedor ou comprador podem liberar a troca.");
        }

        var actorId = isSeller ? trade.SellerId : trade.BuyerId;
        var actorWallet = await _walletProvider.GetByUserIdAsync(actorId, ct);
        if (actorWallet is null)
        {
            return Result.Fail("Carteira do participante indisponível.");
        }

        if (trade.OnChainTradeId is null)
        {
            return Result.Fail("Troca sem referência on-chain.");
        }

        string txHash;
        try
        {
            txHash = await _escrow.ReleaseAsync(actorWallet, trade.OnChainTradeId.Value, ct);
        }
        catch (Exception ex)
        {
            return Result.Fail("falha on-chain: " + ex.Message);
        }

        try
        {
            trade.MarkLiberada(txHash);
        }
        catch (DomainException ex)
        {
            return Result.Fail(ex.Message);
        }

        await _tradeRepo.UpdateAsync(trade, ct);

        // Doação concluída: Reputation aplica a recompensa (pontos + ajuda) e publica
        // RewardUserEvent (mint do bônus RVM no módulo Token); Notifications avisa o doador.
        if (trade.IsDonation)
        {
            await _eventBus.PublishAsync(
                new DonationCompletedEvent(trade.Id, trade.ListingId, trade.SellerId, trade.BuyerId, trade.Mode.ToString()),
                ct);
        }

        return Result.Ok();
    }
}
