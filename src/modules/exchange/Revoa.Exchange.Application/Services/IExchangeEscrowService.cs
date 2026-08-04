using Revoa.Exchange.Domain.Aggregates.TradeAggregate;
using Revoa.IntegrationContracts.UserWallets;

namespace Revoa.Exchange.Application.Services;

// Porta para orquestrar TODO o on-chain do escrow (EscrowVault + ServiceVoucher + RVM approve).
// Cada método assina com a parte correta (seller/buyer/faucet). Impl Nethereum na Infrastructure.
// Lança exceção em falha on-chain — o command handler captura e converte em Result.Fail.
public interface IExchangeEscrowService
{
    // Endereços lidos de Chain:Contracts (vindos das options da Infrastructure).
    string EscrowVaultAddress { get; }
    string ProductNftAddress { get; }
    string ServiceVoucherAddress { get; }

    // Garante (idempotente) que a faucet tem MINTER_ROLE no ServiceVoucher.
    Task EnsureFaucetMinterRoleAsync(CancellationToken ct = default);

    // Assinado pelo SELLER: createTrade(buyer, total, kind, assetContract, tokenId).
    // Retorna (onChainTradeId lido do evento TradeCreated, txHash).
    Task<(long onChainTradeId, string txHash)> CreateTradeAsync(
        UserWallet sellerWallet,
        string buyerAddr,
        long total,
        TradeKind kind,
        string assetContract,
        long tokenId,
        CancellationToken ct = default);

    // Assinado pelo OWNER (buyer): ERC-20 approve(spender, amount) no RVM. Pular se amount==0.
    Task<string> ApproveRvmAsync(
        UserWallet ownerWallet,
        string spenderAddr,
        long amount,
        CancellationToken ct = default);

    // Assinado pelo BUYER: fundTrade(tradeId).
    Task<string> FundTradeAsync(UserWallet buyerWallet, long onChainTradeId, CancellationToken ct = default);

    // Assinado pela FAUCET (MINTER): mintOnPurchase(to, listingId, expiry).
    // Retorna (voucherId lido do evento VoucherMinted, txHash).
    Task<(long voucherId, string txHash)> MintVoucherAsync(
        long listingId,
        string buyerAddr,
        int expiryDays,
        CancellationToken ct = default);

    // Assinado pelo BUYER: redeem(voucherId).
    Task<string> RedeemAsync(UserWallet buyerWallet, long voucherId, CancellationToken ct = default);

    // Assinado por uma PARTE (seller OU buyer): release(tradeId).
    Task<string> ReleaseAsync(UserWallet partyWallet, long onChainTradeId, CancellationToken ct = default);

    // Assinado por uma PARTE: openDispute(tradeId).
    Task<string> OpenDisputeAsync(UserWallet partyWallet, long onChainTradeId, CancellationToken ct = default);

    // Assinado pela FAUCET (ARBITRATOR_ROLE): claimArbitrator(tradeId, releaseToSeller).
    // A faucet é resolvida internamente (chave em Chain:FaucetPrivateKey), como MintVoucher.
    Task<string> ClaimArbitratorAsync(
        long onChainTradeId,
        bool releaseToSeller,
        CancellationToken ct = default);

    // Assinado por uma PARTE: cancel(tradeId).
    Task<string> CancelAsync(UserWallet partyWallet, long onChainTradeId, CancellationToken ct = default);
}
