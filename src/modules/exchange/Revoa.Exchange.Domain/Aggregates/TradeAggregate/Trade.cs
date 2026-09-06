using Revoa.Abstractions;

namespace Revoa.Exchange.Domain.Aggregates.TradeAggregate;

// Espelha EscrowVault.AssetKind on-chain (0=Product, 1=Service).
public enum TradeKind
{
    Product,
    Service
}

// Espelha ListingMode do Catalog (passado como string pela porta IListingSummaryProvider).
public enum TradeMode
{
    Trade,
    Resell,
    Donate,
    Volunteer
}

// Espelha EscrowVault.State (OOUX 11 = Troca, 12 = DoaÃ§Ã£o = trade total 0).
public enum TradeState
{
    Offered,
    Funded,
    Released,
    Disputed,
    Refunded,
    Cancelled
}

// Aggregate "Troca" (OOUX 11) â€” tambÃ©m cobre doaÃ§Ã£o (OOUX 12) quando Modo âˆˆ {Doar, Voluntariar}
// (total 0). Estados espelham o EscrowVault on-chain. Seller/Buyer embed (Nome/Avatar) anti-N+1.
public class Trade : AggregateRoot
{
    public Guid ListingId { get; private set; }
    public TradeMode Mode { get; private set; }
    public TradeKind Kind { get; private set; }

    public Guid SellerId { get; private set; }
    public string SellerWallet { get; private set; } = string.Empty;
    public string SellerName { get; private set; } = string.Empty;
    public string? SellerAvatarUrl { get; private set; }

    public Guid BuyerId { get; private set; }
    public string BuyerWallet { get; private set; } = string.Empty;
    public string BuyerName { get; private set; } = string.Empty;
    public string? BuyerAvatarUrl { get; private set; }

    public long TotalRvm { get; private set; }
    public string AssetContract { get; private set; } = string.Empty;
    // NFT id (produto) | voucher id (serviÃ§o).
    public long TokenId { get; private set; }

    // Preenchido apÃ³s createTrade on-chain.
    public long? OnChainTradeId { get; private set; }

    public TradeState State { get; private set; }

    public DateTime? FundedAt { get; private set; }
    public DateTime? ReleasedAt { get; private set; }
    public string? DisputeOpenedBy { get; private set; }

    // Auditoria: quem resolveu a disputa (e-mail do árbitro). Espelha DisputeOpenedBy.
    public string? ResolvedBy { get; private set; }

    // ServiÃ§o: voucher foi redeemado (confirmaÃ§Ã£o de prestaÃ§Ã£o).
    public bool VoucherRedeemed { get; private set; }

    public string? LastTxHash { get; private set; }

    private Trade() { }

    public bool IsDonation => Mode is TradeMode.Donate or TradeMode.Volunteer;

    // O trade nasce financiado (compra/doaÃ§Ã£o jÃ¡ funded on-chain no command handler).
    public static Trade Create(
        Guid listingId,
        TradeMode modo,
        TradeKind kind,
        Guid sellerId,
        string sellerWallet,
        string sellerNome,
        string? sellerAvatarUrl,
        Guid buyerId,
        string buyerWallet,
        string buyerNome,
        string? buyerAvatarUrl,
        long totalRvm,
        string assetContract,
        long tokenId,
        long onChainTradeId,
        DateTime fundedAt,
        string? lastTxHash)
    {
        ValidateInvariants(modo, kind, totalRvm, tokenId, sellerId, buyerId, assetContract, sellerWallet, buyerWallet);

        return new Trade
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            Mode = modo,
            Kind = kind,
            SellerId = sellerId,
            SellerWallet = sellerWallet,
            SellerName = string.IsNullOrWhiteSpace(sellerNome) ? "UsuÃ¡rio" : sellerNome,
            SellerAvatarUrl = sellerAvatarUrl,
            BuyerId = buyerId,
            BuyerWallet = buyerWallet,
            BuyerName = string.IsNullOrWhiteSpace(buyerNome) ? "UsuÃ¡rio" : buyerNome,
            BuyerAvatarUrl = buyerAvatarUrl,
            TotalRvm = totalRvm,
            AssetContract = assetContract,
            TokenId = tokenId,
            OnChainTradeId = onChainTradeId,
            State = TradeState.Funded,
            FundedAt = fundedAt,
            LastTxHash = lastTxHash,
            Version = 1
        };
    }

    // TransiÃ§Ã£o reservada p/ fluxo assÃ­ncrono futuro (criar â†’ financiar em passos separados).
    public void MarkFunded(long onChainTradeId, DateTime fundedAt, string? tx)
    {
        if (State != TradeState.Offered)
        {
            throw new DomainException("Apenas trade no estado Ofertada pode ser financiada.");
        }

        OnChainTradeId = onChainTradeId;
        FundedAt = fundedAt;
        State = TradeState.Funded;
        LastTxHash = tx ?? LastTxHash;
    }

    // ServiÃ§o: comprador confirma prestaÃ§Ã£o (redeem do voucher). Estado permanece Financiada.
    public void MarkRedeemed(string? tx)
    {
        if (Kind != TradeKind.Service)
        {
            throw new DomainException("Redeem aplica apenas a serviÃ§os.");
        }

        if (State != TradeState.Funded)
        {
            throw new DomainException("Apenas trade financiada pode ter voucher redeemado.");
        }

        if (VoucherRedeemed)
        {
            throw new DomainException("Voucher jÃ¡ foi redeemado.");
        }

        VoucherRedeemed = true;
        LastTxHash = tx ?? LastTxHash;
    }

    // LiberaÃ§Ã£o cooperativa (seller OU buyer) ou por Ã¡rbitro (resolve dispute).
    public void MarkLiberada(string? tx, string? resolvedBy = null)
    {
        if (State is not (TradeState.Funded or TradeState.Disputed))
        {
            throw new DomainException("LiberaÃ§Ã£o exige trade financiada ou disputada.");
        }

        State = TradeState.Released;
        ReleasedAt = DateTime.UtcNow;
        LastTxHash = tx ?? LastTxHash;
        if (!string.IsNullOrWhiteSpace(resolvedBy))
        {
            ResolvedBy = resolvedBy;
        }
    }

    public void MarkDisputada(string openedBy)
    {
        if (State != TradeState.Funded)
        {
            throw new DomainException("Apenas trade financiada pode entrar em disputa.");
        }

        State = TradeState.Disputed;
        DisputeOpenedBy = string.IsNullOrWhiteSpace(openedBy) ? null : openedBy;
    }

    // Reembolso (cancelamento cooperativo ou Ã¡rbitro decide a favor do comprador).
    public void MarkReembolsada(string? tx, string? resolvedBy = null)
    {
        if (State is not (TradeState.Funded or TradeState.Disputed))
        {
            throw new DomainException("Reembolso exige trade financiada ou disputada.");
        }

        State = TradeState.Refunded;
        LastTxHash = tx ?? LastTxHash;
        if (!string.IsNullOrWhiteSpace(resolvedBy))
        {
            ResolvedBy = resolvedBy;
        }
    }

    public void MarkCancelada(string? tx)
    {
        if (State is not (TradeState.Funded or TradeState.Offered))
        {
            throw new DomainException("Cancelamento exige trade ofertada ou financiada.");
        }

        State = TradeState.Cancelled;
        LastTxHash = tx ?? LastTxHash;
    }

    private static void ValidateInvariants(
        TradeMode modo,
        TradeKind kind,
        long totalRvm,
        long tokenId,
        Guid sellerId,
        Guid buyerId,
        string assetContract,
        string sellerWallet,
        string buyerWallet)
    {
        if (totalRvm < 0)
        {
            throw new DomainException("Total RVM nÃ£o pode ser negativo.");
        }

        // Doar/Voluntariar â‡’ total 0.
        if ((modo == TradeMode.Donate || modo == TradeMode.Volunteer) && totalRvm != 0)
        {
            throw new DomainException("DoaÃ§Ã£o/voluntariado deve ter total 0 RVM.");
        }

        // Produto exige NFT (tokenId) em escrow.
        if (kind == TradeKind.Product && tokenId <= 0)
        {
            throw new DomainException("Produto exige tokenId do NFT (mint-to-escrow).");
        }

        if (sellerId == buyerId)
        {
            throw new DomainException("Vendedor e comprador devem ser usuÃ¡rios diferentes.");
        }

        if (string.IsNullOrWhiteSpace(assetContract))
        {
            throw new DomainException("AssetContract Ã© obrigatÃ³rio.");
        }

        if (string.IsNullOrWhiteSpace(sellerWallet) || string.IsNullOrWhiteSpace(buyerWallet))
        {
            throw new DomainException("Carteiras do vendedor e comprador sÃ£o obrigatÃ³rias.");
        }
    }
}
