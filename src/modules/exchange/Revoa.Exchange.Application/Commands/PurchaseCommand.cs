using MediatR;
using Revoa.Abstractions;
using Revoa.Exchange.Application.Services;
using Revoa.Exchange.Domain.Aggregates.TradeAggregate;
using Revoa.Exchange.Domain.Repositories;
using Revoa.IntegrationContracts.Listings;
using Revoa.IntegrationContracts.UserWallets;

namespace Revoa.Exchange.Application.Commands;

// Compra de produto (trocar/repassar) ou contratação de serviço (trocar).
// Orquestra atomicamente: createTrade (+ mint voucher p/ serviço) → approve RVM → fundTrade.
// O trade nasce financiado (State=Financiada). Doação/voluntariado NÃO passa por aqui (fila).
public sealed record PurchaseCommand(
    Guid BuyerId,
    string BuyerNome,
    string? BuyerAvatarUrl,
    Guid ListingId) : IRequest<Result<string>>;

public class PurchaseCommandHandler : IRequestHandler<PurchaseCommand, Result<string>>
{
    private readonly IListingSummaryProvider _listingProvider;
    private readonly IUserWalletProvider _walletProvider;
    private readonly IExchangeEscrowService _escrow;
    private readonly ITradeRepository _tradeRepo;

    public PurchaseCommandHandler(
        IListingSummaryProvider listingProvider,
        IUserWalletProvider walletProvider,
        IExchangeEscrowService escrow,
        ITradeRepository tradeRepo)
    {
        _listingProvider = listingProvider;
        _walletProvider = walletProvider;
        _escrow = escrow;
        _tradeRepo = tradeRepo;
    }

    public async Task<Result<string>> Handle(PurchaseCommand request, CancellationToken ct)
    {
        var listing = await _listingProvider.GetByIdAsync(request.ListingId, ct);
        if (listing is null)
        {
            return Result<string>.Fail("Anúncio não encontrado.");
        }

        if (!string.Equals(listing.Status, "Ativo", StringComparison.OrdinalIgnoreCase))
        {
            return Result<string>.Fail("Anúncio não está ativo.");
        }

        if (listing.IsDonation)
        {
            return Result<string>.Fail("Anúncio de doação/voluntariado usa a fila de ajuda.");
        }

        if (!Enum.TryParse<TradeModo>(listing.Modo, out var modoEnum))
        {
            return Result<string>.Fail("Modo do anúncio inválido.");
        }

        if (!Enum.TryParse(listing.Kind, out TradeKind kind))
        {
            return Result<string>.Fail("Kind do anúncio inválido.");
        }

        // Guarda anti-compra-dupla: um listing tem um único NFT/voucher em escrow. Uma 2ª compra
        // reverte on-chain ("Smart contract error") porque o ativo já foi consumido pela 1ª troca.
        // Rejeita se já existe troca ativa/concluída (só Cancelada libera o listing de novo).
        var existing = await _tradeRepo.GetByListingAsync(request.ListingId, ct);
        if (existing.Any(t => t.State != TradeState.Cancelada))
        {
            return Result<string>.Fail("Este anúncio já está em uma troca ou já foi concluído.");
        }

        var sellerWallet = await _walletProvider.GetByUserIdAsync(listing.VendedorId, ct);
        var buyerWallet = await _walletProvider.GetByUserIdAsync(request.BuyerId, ct);
        if (sellerWallet is null || buyerWallet is null)
        {
            return Result<string>.Fail("Carteira indisponível.");
        }

        var total = listing.PrecoRvm;
        long onChainTradeId;
        long tokenId;
        string assetContract;
        string? lastTxHash = null;

        try
        {
            if (kind == TradeKind.Product)
            {
                tokenId = listing.NftTokenId ?? 0;
                assetContract = _escrow.ProductNftAddress;

                (onChainTradeId, _) = await _escrow.CreateTradeAsync(
                    sellerWallet, buyerWallet.Address, total, kind, assetContract, tokenId, ct);

                if (total > 0)
                {
                    await _escrow.ApproveRvmAsync(buyerWallet, _escrow.EscrowVaultAddress, total, ct);
                }

                lastTxHash = await _escrow.FundTradeAsync(buyerWallet, onChainTradeId, ct);
            }
            else
            {
                await _escrow.EnsureFaucetMinterRoleAsync(ct);

                (var voucherId, _) = await _escrow.MintVoucherAsync(
                    ToOnChainListingId(request.ListingId),
                    buyerWallet.Address,
                    listing.VoucherExpiryDays,
                    ct);

                tokenId = voucherId;
                assetContract = _escrow.ServiceVoucherAddress;

                (onChainTradeId, _) = await _escrow.CreateTradeAsync(
                    sellerWallet, buyerWallet.Address, total, kind, assetContract, tokenId, ct);

                if (total > 0)
                {
                    await _escrow.ApproveRvmAsync(buyerWallet, _escrow.EscrowVaultAddress, total, ct);
                }

                lastTxHash = await _escrow.FundTradeAsync(buyerWallet, onChainTradeId, ct);
            }
        }
        catch (DomainException ex)
        {
            return Result<string>.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail("falha on-chain: " + ex.Message);
        }

        var trade = Trade.Create(
            request.ListingId,
            modoEnum,
            kind,
            listing.VendedorId,
            sellerWallet.Address,
            listing.VendedorNome,
            listing.VendedorAvatarUrl,
            request.BuyerId,
            buyerWallet.Address,
            request.BuyerNome,
            request.BuyerAvatarUrl,
            total,
            assetContract,
            tokenId,
            onChainTradeId,
            DateTime.UtcNow,
            lastTxHash);

        await _tradeRepo.AddAsync(trade, ct);

        return Result<string>.Ok(trade.Id.ToString());
    }

    // Mesma derivação do Catalog: 8 bytes do Guid → long (referência numérica on-chain estável).
    private static long ToOnChainListingId(Guid id)
    {
        var bytes = id.ToByteArray();
        var v = BitConverter.ToInt64(bytes, 0);
        return Math.Abs(v);
    }
}
