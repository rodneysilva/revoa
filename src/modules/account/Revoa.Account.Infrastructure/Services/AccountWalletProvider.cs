using Revoa.Account.Domain.Repositories;
using Revoa.IntegrationContracts.UserWallets;

namespace Revoa.Account.Infrastructure.Services;

// Adapter de IUserWalletProvider: lê o aggregate UserAccount (coleção Accounts) e devolve as
// credenciais da EOA. Mantém o isolamento — o módulo Exchange consome só o port, sem referenciar
// o repositório do Account. MVP dev: chave privada em texto (provisório, ADR-0002).
public class AccountWalletProvider : IUserWalletProvider
{
    private readonly IAccountRepository _accounts;

    public AccountWalletProvider(IAccountRepository accounts)
    {
        _accounts = accounts;
    }

    public async Task<UserWallet?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var account = await _accounts.GetByUserIdAsync(userId, ct);
        if (account is null)
        {
            return null;
        }

        return new UserWallet(account.UserId, account.WalletAddress, account.PrivateKey);
    }
}
