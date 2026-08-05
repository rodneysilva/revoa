using MediatR;
using Revoa.Abstractions;
using Revoa.Coupon.Application.Services;
using Revoa.IntegrationContracts.UserWallets;

namespace Revoa.Coupon.Application.Commands;

// Resgata cupom (UF-29): usuário verificado assina redeem(code) on-chain; minta RVM na própria carteira.
// O on-chain é source of truth (maxUses/usedBy/expiry/revoked) — não há gravação off-chain do resgate.
// Gate Verified no controller; userId do token. Wallet via IUserWalletProvider (porta, isolamento).
public sealed record RedeemCouponCommand(Guid UserId, string Code) : IRequest<Result>;

public class RedeemCouponCommandHandler : IRequestHandler<RedeemCouponCommand, Result>
{
    private readonly IUserWalletProvider _walletProvider;
    private readonly ICouponChainService _chain;

    public RedeemCouponCommandHandler(IUserWalletProvider walletProvider, ICouponChainService chain)
    {
        _walletProvider = walletProvider;
        _chain = chain;
    }

    public async Task<Result> Handle(RedeemCouponCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return Result.Fail("Informe o código do cupom.");
        }

        var wallet = await _walletProvider.GetByUserIdAsync(request.UserId, ct);
        if (wallet is null)
        {
            return Result.Fail("Carteira indisponível.");
        }

        try
        {
            await _chain.RedeemAsync(wallet, request.Code.Trim(), ct);
            return Result.Ok();
        }
        catch (CouponChainException ex)
        {
            return Result.Fail(ex.Error switch
            {
                CouponChainError.NotFound => "Cupom inválido.",
                CouponChainError.Expired => "Cupom expirado.",
                CouponChainError.AlreadyUsed => "Você já resgatou este cupom.",
                CouponChainError.MaxUsesReached => "Cupom esgotado.",
                CouponChainError.Revoked => "Cupom revogado.",
                _ => "Falha ao resgatar cupom."
            });
        }
    }
}
