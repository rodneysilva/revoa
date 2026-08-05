using MediatR;
using Revoa.Abstractions;
using Revoa.Exchange.Application.Services;
using Revoa.Exchange.Domain.Aggregates.TradeAggregate;
using Revoa.Exchange.Domain.Repositories;
using Revoa.IntegrationContracts.UserWallets;

namespace Revoa.Exchange.Application.Commands;

// Cancelamento cooperativo (seller OU buyer): cancel on-chain (reembolsa + devolve/queima ativo).
public sealed record CancelTradeCommand(
    Guid ActorId,
    Guid TradeId) : IRequest<Result>;

public class CancelTradeCommandHandler : IRequestHandler<CancelTradeCommand, Result>
{
    private readonly ITradeRepository _tradeRepo;
    private readonly IUserWalletProvider _walletProvider;
    private readonly IExchangeEscrowService _escrow;

    public CancelTradeCommandHandler(
        ITradeRepository tradeRepo,
        IUserWalletProvider walletProvider,
        IExchangeEscrowService escrow)
    {
        _tradeRepo = tradeRepo;
        _walletProvider = walletProvider;
        _escrow = escrow;
    }

    public async Task<Result> Handle(CancelTradeCommand request, CancellationToken ct)
    {
        var trade = await _tradeRepo.GetByIdAsync(request.TradeId, ct);
        if (trade is null)
        {
            return Result.Fail("Troca não encontrada.");
        }

        var isSeller = trade.SellerId == request.ActorId;
        var isBuyer = trade.BuyerId == request.ActorId;
        if (!isSeller && !isBuyer)
        {
            return Result.Fail("Apenas vendedor ou comprador podem cancelar a troca.");
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
            txHash = await _escrow.CancelAsync(actorWallet, trade.OnChainTradeId.Value, ct);
        }
        catch (Exception ex)
        {
            return Result.Fail("falha on-chain: " + ex.Message);
        }

        try
        {
            trade.MarkCancelada(txHash);
        }
        catch (DomainException ex)
        {
            return Result.Fail(ex.Message);
        }

        await _tradeRepo.UpdateAsync(trade, ct);

        return Result.Ok();
    }
}
