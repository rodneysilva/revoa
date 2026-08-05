using MediatR;
using Revoa.Abstractions;
using Revoa.Exchange.Application.Services;
using Revoa.Exchange.Domain.Aggregates.TradeAggregate;
using Revoa.Exchange.Domain.Repositories;
using Revoa.IntegrationContracts.UserWallets;

namespace Revoa.Exchange.Application.Commands;

// Serviço: comprador confirma a prestação (redeem do voucher on-chain). Só Kind=Service financiado.
public sealed record RedeemVoucherCommand(
    Guid BuyerId,
    Guid TradeId) : IRequest<Result>;

public class RedeemVoucherCommandHandler : IRequestHandler<RedeemVoucherCommand, Result>
{
    private readonly ITradeRepository _tradeRepo;
    private readonly IUserWalletProvider _walletProvider;
    private readonly IExchangeEscrowService _escrow;

    public RedeemVoucherCommandHandler(
        ITradeRepository tradeRepo,
        IUserWalletProvider walletProvider,
        IExchangeEscrowService escrow)
    {
        _tradeRepo = tradeRepo;
        _walletProvider = walletProvider;
        _escrow = escrow;
    }

    public async Task<Result> Handle(RedeemVoucherCommand request, CancellationToken ct)
    {
        var trade = await _tradeRepo.GetByIdAsync(request.TradeId, ct);
        if (trade is null)
        {
            return Result.Fail("Troca não encontrada.");
        }

        if (trade.Kind != TradeKind.Service)
        {
            return Result.Fail("Redeem aplica apenas a serviços.");
        }

        if (trade.State != TradeState.Financiada)
        {
            return Result.Fail("Apenas troca financiada permite redeem.");
        }

        if (trade.BuyerId != request.BuyerId)
        {
            return Result.Fail("Apenas o comprador pode confirmar a prestação do serviço.");
        }

        var buyerWallet = await _walletProvider.GetByUserIdAsync(trade.BuyerId, ct);
        if (buyerWallet is null)
        {
            return Result.Fail("Carteira do comprador indisponível.");
        }

        string txHash;
        try
        {
            txHash = await _escrow.RedeemAsync(buyerWallet, trade.TokenId, ct);
        }
        catch (Exception ex)
        {
            return Result.Fail("falha on-chain: " + ex.Message);
        }

        try
        {
            trade.MarkRedeemed(txHash);
        }
        catch (DomainException ex)
        {
            return Result.Fail(ex.Message);
        }

        await _tradeRepo.UpdateAsync(trade, ct);

        return Result.Ok();
    }
}
