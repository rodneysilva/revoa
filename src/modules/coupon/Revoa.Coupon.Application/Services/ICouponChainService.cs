using System.Numerics;
using Revoa.IntegrationContracts.UserWallets;

namespace Revoa.Coupon.Application.Services;

// Porta para orquestrar o on-chain do CouponRedeemer (criar/revogar pelo admin; resgatar pelo usuário).
// Cada método assina com a parte correta (faucet p/ admin; carteira do usuário p/ resgate). Impl Nethereum
// na Infrastructure. Lança exceção em falha on-chain — o command handler captura e converte em Result.Fail.
public interface ICouponChainService
{
    // Garante (idempotente) que a faucet tem COUPON_ADMIN_ROLE no CouponRedeemer.
    Task EnsureFaucetCouponAdminRoleAsync(CancellationToken ct = default);

    // Assinado pela FAUCET (COUPON_ADMIN_ROLE): createCoupon(code, amount, maxUses, expiryUnix).
    // `amount` já em unidades RAW do RVM (18 decimais) — 10 RVM = 10×10^18 (estoura long).
    // Retorna (codeHash lido do evento CouponCreated, txHash).
    Task<(string codeHash, string txHash)> CreateCouponAsync(
        string code,
        BigInteger amount,
        int maxUses,
        long expiryUnix,
        CancellationToken ct = default);

    // Assinado pela FAUCET (COUPON_ADMIN_ROLE): revokeCoupon(code).
    Task<string> RevokeCouponAsync(string code, CancellationToken ct = default);

    // Assinado pelo USUÁRIO (msg.sender = dono da carteira; minta RVM p/ ele): redeem(code).
    Task<string> RedeemAsync(UserWallet userWallet, string code, CancellationToken ct = default);
}
