using System.Numerics;

namespace Revoa.Token.Application.Services;

// Porta para operar o contrato RVM (ERC-20). Implementação Nethereum no Infrastructure.
public interface IRvmService
{
    Task<BigInteger> BalanceOfAsync(string address, CancellationToken ct = default);

    Task<string> MintAsync(string to, BigInteger amount, CancellationToken ct = default);

    /// <summary>
    /// Garante (idempotente) que a faucet account tem MINTER_ROLE no RVM.
    /// </summary>
    Task EnsureFaucetMinterRoleAsync(CancellationToken ct = default);

    /// <summary>
    /// Envia ETH (gas) da faucet para um endereço se a flag Chain:FundWalletGasOnCreate estiver
    /// ativa (DEV/anvil — carteiras EOA precisam de gas; em produção o gas é do Paymaster/AA
    /// relayer). No-op em produção. Resiliente (falhas só logam).
    /// </summary>
    Task FundGasIfEnabledAsync(string toAddress, CancellationToken ct = default);
}
