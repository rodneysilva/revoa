using Revoa.Abstractions;

namespace Revoa.Account.Domain.Aggregates.AccountAggregate;

// PROVISÓRIO (MVP dev): a chave privada é guardada em texto no aggregate.
// A Account Abstraction (Safe + ERC-4337 + WebAuthn/passkey) substitui isto depois:
// o usuário terá self-custody real (carteira invisível via passkey), e a chave EOA
// deixará de existir. NÃO levar isto para produção. Ver ADR-0002 / docs/ARCHITECTURE.md.
//
// Tipo nomeado "UserAccount" (e não "Account") para evitar colisão com o namespace Revoa.Account.
public class UserAccount : AggregateRoot
{
    public Guid UserId { get; private set; }
    public string WalletAddress { get; private set; } = string.Empty;

    // PROVISÓRIO: chave privada da EOA em texto. Criptografia/KMS virá com a AA real.
    public string PrivateKey { get; private set; } = string.Empty;

    private UserAccount()
    {
    }

    public static UserAccount Create(Guid userId, string walletAddress, string privateKey)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("UserId é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(walletAddress))
        {
            throw new DomainException("WalletAddress é obrigatória.");
        }

        if (string.IsNullOrWhiteSpace(privateKey))
        {
            throw new DomainException("PrivateKey é obrigatória (MVP dev).");
        }

        return new UserAccount
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            WalletAddress = walletAddress,
            PrivateKey = privateKey,
            Version = 1
        };
    }
}
