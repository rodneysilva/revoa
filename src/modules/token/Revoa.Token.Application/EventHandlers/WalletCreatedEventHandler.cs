using System.Numerics;
using MediatR;
using Microsoft.Extensions.Logging;
using Revoa.IntegrationContracts.Events;
using Revoa.Token.Application.Services;
using Revoa.Token.Domain;

namespace Revoa.Token.Application.EventHandlers;

// Reage à criação da carteira (Account) e dispara o faucet: minta R$20 (20 RVM) para o usuário.
// Cadeia: WalletCreatedEvent → [aqui] RVM mint (faucet) → saldo disponível na carteira do usuário.
public class WalletCreatedEventHandler : INotificationHandler<WalletCreatedEvent>
{
    private static readonly BigInteger FaucetAmount =
        BigInteger.Multiply(RvmConstants.FaucetUnits, BigInteger.Pow(10, RvmConstants.Decimals));

    private readonly IRvmService _rvm;
    private readonly ILogger<WalletCreatedEventHandler> _logger;

    public WalletCreatedEventHandler(IRvmService rvm, ILogger<WalletCreatedEventHandler> logger)
    {
        _rvm = rvm;
        _logger = logger;
    }

    public async Task Handle(WalletCreatedEvent notification, CancellationToken ct)
    {
        // O mint on-chain não deve ser cancelado pelo token do request HTTP (timeout do cliente
        // não deve abortar a tx). Usa CancellationToken.None + tratamento resiliente: falhas de
        // faucet são logadas e não quebram o cadastro (o usuário pode receber o crédito depois).
        try
        {
            // Garante a role antes de mintar (idempotente). Faucet = DEFAULT_ADMIN (deployer) em dev.
            await _rvm.EnsureFaucetMinterRoleAsync(CancellationToken.None);

            var txHash = await _rvm.MintAsync(notification.WalletAddress, FaucetAmount, CancellationToken.None);

            // DEV: financia a nova carteira com ETH (gas) — sem isso as txs do usuário falham
            // ("insufficient gas"). No-op em produção (gas vem do Paymaster/AA relayer).
            try
            {
                await _rvm.FundGasIfEnabledAsync(notification.WalletAddress, CancellationToken.None);
            }
            catch (Exception gasEx)
            {
                _logger.LogWarning(gasEx, "Faucet gas FALHOU para {Address} (mint OK).", notification.WalletAddress);
            }

            _logger.LogInformation(
                "Faucet: mintados {Units} RVM ({Raw} raw) para {Address} (UserId={UserId}) tx={TxHash}",
                RvmConstants.FaucetUnits, FaucetAmount.ToString(), notification.WalletAddress,
                notification.UserId, txHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Faucet FALHOU para {Address} (UserId={UserId}). O cadastro continuou; reprocessar o mint depois.",
                notification.WalletAddress, notification.UserId);
        }
    }
}
