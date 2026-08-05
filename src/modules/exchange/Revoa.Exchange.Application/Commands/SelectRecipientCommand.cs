using MediatR;
using Revoa.Abstractions;
using Revoa.Exchange.Application.Services;
using Revoa.Exchange.Domain.Aggregates.TradeAggregate;
using Revoa.Exchange.Domain.Repositories;
using Revoa.IntegrationContracts.Listings;
using Revoa.IntegrationContracts.UserWallets;

namespace Revoa.Exchange.Application.Commands;

// Curadoria: doador escolhe o receptor na fila de doação/voluntariado. Cria trade total=0
// (doação on-chain: createTrade + fundTrade(0), sem approve). Liga o HelpRequest à Trade.
public sealed record SelectRecipientCommand(
    Guid SellerId,
    Guid HelpRequestId) : IRequest<Result<string>>;

public class SelectRecipientCommandHandler : IRequestHandler<SelectRecipientCommand, Result<string>>
{
    private readonly IHelpRequestRepository _helpRepo;
    private readonly IListingSummaryProvider _listingProvider;
    private readonly IUserWalletProvider _walletProvider;
    private readonly IExchangeEscrowService _escrow;
    private readonly ITradeRepository _tradeRepo;

    public SelectRecipientCommandHandler(
        IHelpRequestRepository helpRepo,
        IListingSummaryProvider listingProvider,
        IUserWalletProvider walletProvider,
        IExchangeEscrowService escrow,
        ITradeRepository tradeRepo)
    {
        _helpRepo = helpRepo;
        _listingProvider = listingProvider;
        _walletProvider = walletProvider;
        _escrow = escrow;
        _tradeRepo = tradeRepo;
    }

    public async Task<Result<string>> Handle(SelectRecipientCommand request, CancellationToken ct)
    {
        var help = await _helpRepo.GetByIdAsync(request.HelpRequestId, ct);
        if (help is null)
        {
            return Result<string>.Fail("Pedido de ajuda não encontrado.");
        }

        if (help.State != Domain.Aggregates.HelpRequestAggregate.HelpRequestState.Open)
        {
            return Result<string>.Fail("Pedido de ajuda já foi atendido ou retirado.");
        }

        var listing = await _listingProvider.GetByIdAsync(help.ListingId, ct);
        if (listing is null)
        {
            return Result<string>.Fail("Anúncio não encontrado.");
        }

        // Só o dono do anúncio (doador) escolhe o receptor.
        if (listing.VendedorId != request.SellerId)
        {
            return Result<string>.Fail("Apenas o doador pode selecionar o receptor.");
        }

        if (!listing.IsDonation)
        {
            return Result<string>.Fail("Apenas anúncios de doação/voluntariado aceitam seleção.");
        }

        // Guarda anti-doação-dupla: o NFT/voucher é único. Rejeita se já existe troca ativa/concluída
        // para este listing (evita revert on-chain ao tentar doar o mesmo item duas vezes).
        var existing = await _tradeRepo.GetByListingAsync(help.ListingId, ct);
        if (existing.Any(t => t.State != TradeState.Cancelada))
        {
            return Result<string>.Fail("Este anúncio já foi doado ou está em doação.");
        }

        var sellerWallet = await _walletProvider.GetByUserIdAsync(request.SellerId, ct);
        var buyerWallet = await _walletProvider.GetByUserIdAsync(help.AuthorId, ct);
        if (sellerWallet is null || buyerWallet is null)
        {
            return Result<string>.Fail("Carteira indisponível.");
        }

        if (!Enum.TryParse<TradeModo>(listing.Modo, out var modoEnum))
        {
            return Result<string>.Fail("Modo do anúncio inválido.");
        }

        if (!Enum.TryParse(listing.Kind, out TradeKind kind))
        {
            return Result<string>.Fail("Kind do anúncio inválido.");
        }

        long tokenId;
        string assetContract;
        long onChainTradeId;
        string? lastTxHash = null;

        try
        {
            if (kind == TradeKind.Service)
            {
                // Voluntariado: mint voucher antes do createTrade.
                await _escrow.EnsureFaucetMinterRoleAsync(ct);
                (tokenId, _) = await _escrow.MintVoucherAsync(
                    ToOnChainListingId(help.ListingId),
                    buyerWallet.Address,
                    listing.VoucherExpiryDays,
                    ct);
                assetContract = _escrow.ServiceVoucherAddress;
            }
            else
            {
                // Doar produto: NFT já está no escrow (mint-to-escrow ao listar).
                tokenId = listing.NftTokenId ?? 0;
                assetContract = _escrow.ProductNftAddress;
            }

            (onChainTradeId, _) = await _escrow.CreateTradeAsync(
                sellerWallet, buyerWallet.Address, 0, kind, assetContract, tokenId, ct);

            // total=0: fundTrade vira Funded sem transferFrom (sem approve).
            lastTxHash = await _escrow.FundTradeAsync(buyerWallet, onChainTradeId, ct);
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
            help.ListingId,
            modoEnum,
            kind,
            request.SellerId,
            sellerWallet.Address,
            listing.VendedorNome,
            listing.VendedorAvatarUrl,
            help.AuthorId,
            buyerWallet.Address,
            help.AuthorNome,
            help.AuthorAvatarUrl,
            0,
            assetContract,
            tokenId,
            onChainTradeId,
            DateTime.UtcNow,
            lastTxHash);

        await _tradeRepo.AddAsync(trade, ct);

        try
        {
            help.Select(trade.Id);
            await _helpRepo.UpdateAsync(help, ct);
        }
        catch (DomainException ex)
        {
            return Result<string>.Fail(ex.Message);
        }

        return Result<string>.Ok(trade.Id.ToString());
    }

    private static long ToOnChainListingId(Guid id)
    {
        var bytes = id.ToByteArray();
        var v = BitConverter.ToInt64(bytes, 0);
        return Math.Abs(v);
    }
}
