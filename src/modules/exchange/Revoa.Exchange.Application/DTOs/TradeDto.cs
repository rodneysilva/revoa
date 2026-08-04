using Revoa.Exchange.Domain.Aggregates.HelpRequestAggregate;
using Revoa.Exchange.Domain.Aggregates.TradeAggregate;

namespace Revoa.Exchange.Application.DTOs;

// Detalhe completo de uma troca/doação (GET /api/trades/{id} e histórico).
public sealed record TradeDto(
    Guid Id,
    Guid ListingId,
    string Modo,
    string Kind,
    Guid SellerId,
    string SellerWallet,
    string SellerNome,
    string? SellerAvatarUrl,
    Guid BuyerId,
    string BuyerWallet,
    string BuyerNome,
    string? BuyerAvatarUrl,
    long TotalRvm,
    string AssetContract,
    long TokenId,
    long? OnChainTradeId,
    string State,
    DateTime? FundedAt,
    DateTime? ReleasedAt,
    string? DisputeOpenedBy,
    bool VoucherRedeemed,
    string? LastTxHash,
    long Version,
    bool IsDonation);

public sealed record HelpRequestDto(
    Guid Id,
    Guid ListingId,
    Guid AuthorId,
    string AuthorNome,
    string? AuthorAvatarUrl,
    string Mensagem,
    string State,
    DateTime CreatedAt,
    Guid? SelectedTradeId);

public static class TradeDtoMapper
{
    public static TradeDto From(Trade t)
    {
        return new TradeDto(
            t.Id,
            t.ListingId,
            t.Modo.ToString(),
            t.Kind.ToString(),
            t.SellerId,
            t.SellerWallet,
            t.SellerNome,
            t.SellerAvatarUrl,
            t.BuyerId,
            t.BuyerWallet,
            t.BuyerNome,
            t.BuyerAvatarUrl,
            t.TotalRvm,
            t.AssetContract,
            t.TokenId,
            t.OnChainTradeId,
            t.State.ToString(),
            t.FundedAt,
            t.ReleasedAt,
            t.DisputeOpenedBy,
            t.VoucherRedeemed,
            t.LastTxHash,
            t.Version,
            t.IsDonation);
    }
}

public static class HelpRequestDtoMapper
{
    public static HelpRequestDto From(HelpRequest h)
    {
        return new HelpRequestDto(
            h.Id,
            h.ListingId,
            h.AuthorId,
            h.AuthorNome,
            h.AuthorAvatarUrl,
            h.Mensagem,
            h.State.ToString(),
            h.CreatedAt,
            h.SelectedTradeId);
    }
}
