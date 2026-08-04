using System.Numerics;

namespace Revoa.Catalog.Application.Services;

// Porta para o contrato ProductNFT (ERC-721). mint-to-escrow ao listar produto (UF-07..09).
// Implementação Nethereum no Infrastructure.
public interface IProductNftService
{
    /// <summary>
    /// Mint o NFT do produto direto no EscrowVault (nunca na carteira do vendedor).
    /// Retorna o tokenId gerado.
    /// </summary>
    Task<BigInteger> MintToEscrowAsync(long listingId, string tokenUri, CancellationToken ct = default);

    /// <summary>
    /// Garante (idempotente) que a faucet tem MINTER_ROLE no ProductNFT.
    /// </summary>
    Task EnsureFaucetMinterRoleAsync(CancellationToken ct = default);
}
