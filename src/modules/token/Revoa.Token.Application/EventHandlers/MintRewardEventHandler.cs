using System.Numerics;
using MediatR;
using Microsoft.Extensions.Logging;
using Revoa.IntegrationContracts.Events;
using Revoa.IntegrationContracts.UserWallets;
using Revoa.Token.Application.Services;

namespace Revoa.Token.Application.EventHandlers;

// Consome RewardUserEvent (publicado pelo Reputation) e minta o bônus RVM na carteira do
// usuário via faucet. Isolamento: Token só conhece o evento + a porta IUserWalletProvider,
// nunca o módulo Reputation. Resiliente: falhas de mint são logadas e não propagam.
public class MintRewardEventHandler : INotificationHandler<RewardUserEvent>
{
    private static readonly BigInteger RvmRawBase = BigInteger.Pow(10, 18);

    private readonly IRvmService _rvm;
    private readonly IUserWalletProvider _wallets;
    private readonly ILogger<MintRewardEventHandler> _logger;

    public MintRewardEventHandler(
        IRvmService rvm,
        IUserWalletProvider wallets,
        ILogger<MintRewardEventHandler> logger)
    {
        _rvm = rvm;
        _wallets = wallets;
        _logger = logger;
    }

    public async Task Handle(RewardUserEvent notification, CancellationToken ct)
    {
        try
        {
            var wallet = await _wallets.GetByUserIdAsync(notification.UserId, ct);
            if (wallet is null)
            {
                _logger.LogWarning(
                    "Bônus RVM: carteira não encontrada para o usuário {UserId} (motivo: {Reason}). Mint ignorado.",
                    notification.UserId, notification.Reason);
                return;
            }

            await _rvm.EnsureFaucetMinterRoleAsync(ct);

            var raw = BigInteger.Multiply(notification.AmountRvm, RvmRawBase);
            var txHash = await _rvm.MintAsync(wallet.Address, raw, ct);

            _logger.LogInformation(
                "Bônus RVM: mintados {Amount} RVM ({Raw} raw) para {Address} (UserId={UserId}, motivo={Reason}) tx={TxHash}",
                notification.AmountRvm, raw.ToString(), wallet.Address, notification.UserId,
                notification.Reason, txHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Bônus RVM FALHOU para o usuário {UserId} (motivo: {Reason}). Reprocessar depois.",
                notification.UserId, notification.Reason);
        }
    }
}
