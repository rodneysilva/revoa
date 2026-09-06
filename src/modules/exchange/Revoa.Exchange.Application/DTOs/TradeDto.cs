using Revoa.Exchange.Domain.Aggregates.HelpRequestAggregate;
using Revoa.Exchange.Domain.Aggregates.TradeAggregate;

namespace Revoa.Exchange.Application.DTOs;

// Troca/doação em formato público (GET /api/trades/{id} e histórico — anônimo vê, UF-11/12).
// SEM carteiras (SellerWallet/BuyerWallet), AssetContract, TokenId, OnChainTradeId, LastTxHash e
// Version: dados sensíveis/on-chain sem consumidor no contrato público da API. O estado
// financeiro autoritativo é on-chain; quem precisa dos hashes consulta o explorer da chain.
public sealed record TradeSummaryDto(
    Guid Id,
    Guid ListingId,
    string Mode,
    string Kind,
    Guid SellerId,
    string SellerName,
    string? SellerAvatarUrl,
    Guid BuyerId,
    string BuyerName,
    string? BuyerAvatarUrl,
    long TotalRvm,
    string State,
    DateTime? FundedAt,
    DateTime? ReleasedAt,
    string? DisputeOpenedBy,
    bool VoucherRedeemed,
    bool IsDonation);

public sealed record HelpRequestDto(
    Guid Id,
    Guid ListingId,
    Guid AuthorId,
    string AuthorName,
    string? AuthorAvatarUrl,
    string Message,
    string State,
    DateTime CreatedAt,
    Guid? SelectedTradeId);

public static class TradeDtoMapper
{
    public static TradeSummaryDto From(Trade t)
    {
        return new TradeSummaryDto(
            t.Id,
            t.ListingId,
            t.Mode.ToString(),
            t.Kind.ToString(),
            t.SellerId,
            t.SellerName,
            t.SellerAvatarUrl,
            t.BuyerId,
            t.BuyerName,
            t.BuyerAvatarUrl,
            t.TotalRvm,
            t.State.ToString(),
            t.FundedAt,
            t.ReleasedAt,
            t.DisputeOpenedBy,
            t.VoucherRedeemed,
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
            h.AuthorName,
            h.AuthorAvatarUrl,
            h.Message,
            h.State.ToString(),
            h.CreatedAt,
            h.SelectedTradeId);
    }
}
