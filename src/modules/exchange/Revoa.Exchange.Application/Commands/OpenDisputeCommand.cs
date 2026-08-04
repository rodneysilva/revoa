using MediatR;
using Revoa.Abstractions;
using Revoa.Exchange.Application.Services;
using Revoa.Exchange.Domain.Aggregates.TradeAggregate;
using Revoa.Exchange.Domain.Repositories;
using Revoa.IntegrationContracts.UserWallets;

namespace Revoa.Exchange.Application.Commands;

// Abre disputa (seller OU buyer) dentro da janela de 72h. Registra quem abriu (nome).
public sealed record OpenDisputeCommand(
    Guid ActorId,
    string ActorNome,
    Guid TradeId) : IRequest<Result>;

public class OpenDisputeCommandHandler : IRequestHandler<OpenDisputeCommand, Result>
{
    private readonly ITradeRepository _tradeRepo;
    private readonly IUserWalletProvider _walletProvider;
    private readonly IExchangeEscrowService _escrow;

    public OpenDisputeCommandHandler(
        ITradeRepository tradeRepo,
        IUserWalletProvider walletProvider,
        IExchangeEscrowService escrow)
    {
        _tradeRepo = tradeRepo;
        _walletProvider = walletProvider;
        _escrow = escrow;
    }

    public async Task<Result> Handle(OpenDisputeCommand request, CancellationToken ct)
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
            return Result.Fail("Apenas vendedor ou comprador podem abrir disputa.");
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
            txHash = await _escrow.OpenDisputeAsync(actorWallet, trade.OnChainTradeId.Value, ct);
        }
        catch (Exception ex)
        {
            return Result.Fail("falha on-chain: " + ex.Message);
        }

        try
        {
            trade.MarkDisputada(string.IsNullOrWhiteSpace(request.ActorNome) ? actorWallet.Address : request.ActorNome);
        }
        catch (DomainException ex)
        {
            return Result.Fail(ex.Message);
        }

        await _tradeRepo.UpdateAsync(trade, ct);

        return Result.Ok();
    }
}
