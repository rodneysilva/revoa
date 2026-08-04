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
}
