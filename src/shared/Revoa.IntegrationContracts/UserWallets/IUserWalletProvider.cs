namespace Revoa.IntegrationContracts.UserWallets;

// Porta (anti-corruption layer): permite que outros módulos (ex.: Exchange) assinem transações
// on-chain em nome de um usuário SEM acessar a coleção Accounts diretamente (isolamento de módulos:
// "nunca importar repository de outro módulo"). O adapter vive em Revoa.Account.Infrastructure,
// que lê o aggregate UserAccount e devolve as credenciais da carteira.
//
// MVP DEV: a chave privada da EOA é guardada em texto (provisório, ADR-0002). Com a Account
// Abstraction real (Safe + ERC-4337 + WebAuthn/passkey) este port evolui para um signer/relayer
// que assina UserOps internamente sem expor a chave privada — o consumidor não enxerga a diferença.
public sealed record UserWallet(Guid UserId, string Address, string PrivateKey);

public interface IUserWalletProvider
{
    // Retorna a carteira do usuário ou null se ainda não foi criada (ex.: faucet não rodou).
    Task<UserWallet?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
}
