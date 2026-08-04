using Revoa.Abstractions;

namespace Revoa.Exchange.Domain.Aggregates.TradeAggregate;

// Espelha EscrowVault.AssetKind on-chain (0=Product, 1=Service).
public enum TradeKind
{
    Product,
    Service
}

// Espelha ListingModo do Catalog (passado como string pela porta IListingSummaryProvider).
public enum TradeModo
{
    Trocar,
    Repassar,
    Doar,
    Voluntariar
}

// Espelha EscrowVault.State (OOUX 11 = Troca, 12 = Doação = trade total 0).
public enum TradeState
{
    Ofertada,
    Financiada,
    Liberada,
    Disputada,
    Reembolsada,
    Cancelada
}

// Aggregate "Troca" (OOUX 11) — também cobre doação (OOUX 12) quando Modo ∈ {Doar, Voluntariar}
// (total 0). Estados espelham o EscrowVault on-chain. Seller/Buyer embed (Nome/Avatar) anti-N+1.
public class Trade : AggregateRoot
{
    public Guid ListingId { get; private set; }
    public TradeModo Modo { get; private set; }
    public TradeKind Kind { get; private set; }

    public Guid SellerId { get; private set; }
    public string SellerWallet { get; private set; } = string.Empty;
    public string SellerNome { get; private set; } = string.Empty;
    public string? SellerAvatarUrl { get; private set; }

    public Guid BuyerId { get; private set; }
    public string BuyerWallet { get; private set; } = string.Empty;
    public string BuyerNome { get; private set; } = string.Empty;
    public string? BuyerAvatarUrl { get; private set; }

    public long TotalRvm { get; private set; }
    public string AssetContract { get; private set; } = string.Empty;
    // NFT id (produto) | voucher id (serviço).
    public long TokenId { get; private set; }

    // Preenchido após createTrade on-chain.
    public long? OnChainTradeId { get; private set; }

    public TradeState State { get; private set; }

    public DateTime? FundedAt { get; private set; }
    public DateTime? ReleasedAt { get; private set; }
    public string? DisputeOpenedBy { get; private set; }

    // Serviço: voucher foi redeemado (confirmação de prestação).
    public bool VoucherRedeemed { get; private set; }

    public string? LastTxHash { get; private set; }

    private Trade() { }

    public bool IsDonation => Modo is TradeModo.Doar or TradeModo.Voluntariar;

    // O trade nasce financiado (compra/doação já funded on-chain no command handler).
    public static Trade Create(
        Guid listingId,
        TradeModo modo,
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
            Modo = modo,
            Kind = kind,
            SellerId = sellerId,
            SellerWallet = sellerWallet,
            SellerNome = string.IsNullOrWhiteSpace(sellerNome) ? "Usuário" : sellerNome,
            SellerAvatarUrl = sellerAvatarUrl,
            BuyerId = buyerId,
            BuyerWallet = buyerWallet,
            BuyerNome = string.IsNullOrWhiteSpace(buyerNome) ? "Usuário" : buyerNome,
            BuyerAvatarUrl = buyerAvatarUrl,
            TotalRvm = totalRvm,
            AssetContract = assetContract,
            TokenId = tokenId,
            OnChainTradeId = onChainTradeId,
            State = TradeState.Financiada,
            FundedAt = fundedAt,
            LastTxHash = lastTxHash,
            Version = 1
        };
    }

    // Transição reservada p/ fluxo assíncrono futuro (criar → financiar em passos separados).
    public void MarkFunded(long onChainTradeId, DateTime fundedAt, string? tx)
    {
        if (State != TradeState.Ofertada)
        {
            throw new DomainException("Apenas trade no estado Ofertada pode ser financiada.");
        }

        OnChainTradeId = onChainTradeId;
        FundedAt = fundedAt;
        State = TradeState.Financiada;
        LastTxHash = tx ?? LastTxHash;
        IncrementVersion();
    }

    // Serviço: comprador confirma prestação (redeem do voucher). Estado permanece Financiada.
    public void MarkRedeemed(string? tx)
    {
        if (Kind != TradeKind.Service)
        {
            throw new DomainException("Redeem aplica apenas a serviços.");
        }

        if (State != TradeState.Financiada)
        {
            throw new DomainException("Apenas trade financiada pode ter voucher redeemado.");
        }

        if (VoucherRedeemed)
        {
            throw new DomainException("Voucher já foi redeemado.");
        }

        VoucherRedeemed = true;
        LastTxHash = tx ?? LastTxHash;
        IncrementVersion();
    }

    // Liberação cooperativa (seller OU buyer) ou por árbitro (resolve dispute).
    public void MarkLiberada(string? tx)
    {
        if (State is not (TradeState.Financiada or TradeState.Disputada))
        {
            throw new DomainException("Liberação exige trade financiada ou disputada.");
        }

        State = TradeState.Liberada;
        ReleasedAt = DateTime.UtcNow;
        LastTxHash = tx ?? LastTxHash;
        IncrementVersion();
    }

    public void MarkDisputada(string openedBy)
    {
        if (State != TradeState.Financiada)
        {
            throw new DomainException("Apenas trade financiada pode entrar em disputa.");
        }

        State = TradeState.Disputada;
        DisputeOpenedBy = string.IsNullOrWhiteSpace(openedBy) ? null : openedBy;
        IncrementVersion();
    }

    // Reembolso (cancelamento cooperativo ou árbitro decide a favor do comprador).
    public void MarkReembolsada(string? tx)
    {
        if (State is not (TradeState.Financiada or TradeState.Disputada))
        {
            throw new DomainException("Reembolso exige trade financiada ou disputada.");
        }

        State = TradeState.Reembolsada;
        LastTxHash = tx ?? LastTxHash;
        IncrementVersion();
    }

    public void MarkCancelada(string? tx)
    {
        if (State is not (TradeState.Financiada or TradeState.Ofertada))
        {
            throw new DomainException("Cancelamento exige trade ofertada ou financiada.");
        }

        State = TradeState.Cancelada;
        LastTxHash = tx ?? LastTxHash;
        IncrementVersion();
    }

    private static void ValidateInvariants(
        TradeModo modo,
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
            throw new DomainException("Total RVM não pode ser negativo.");
        }

        // Doar/Voluntariar ⇒ total 0.
        if ((modo == TradeModo.Doar || modo == TradeModo.Voluntariar) && totalRvm != 0)
        {
            throw new DomainException("Doação/voluntariado deve ter total 0 RVM.");
        }

        // Produto exige NFT (tokenId) em escrow.
        if (kind == TradeKind.Product && tokenId <= 0)
        {
            throw new DomainException("Produto exige tokenId do NFT (mint-to-escrow).");
        }

        if (sellerId == buyerId)
        {
            throw new DomainException("Vendedor e comprador devem ser usuários diferentes.");
        }

        if (string.IsNullOrWhiteSpace(assetContract))
        {
            throw new DomainException("AssetContract é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(sellerWallet) || string.IsNullOrWhiteSpace(buyerWallet))
        {
            throw new DomainException("Carteiras do vendedor e comprador são obrigatórias.");
        }
    }
}
