using Revoa.Account.Domain.Repositories;
using Revoa.IntegrationContracts.Accounts;

namespace Revoa.Account.Infrastructure.Services;

// Adapter de IWalletAddressReader: lê o aggregate UserAccount (coleção Accounts) e devolve só
// UserId + Address. Mantém o isolamento — o módulo Demurrage consome só o port, sem referenciar o
// repositório do Account. Espelha AccountWalletProvider (adapter de IUserWalletProvider).
public class WalletAddressReader : IWalletAddressReader
{
    private readonly IAccountRepository _accounts;

    public WalletAddressReader(IAccountRepository accounts)
    {
        _accounts = accounts;
    }

    public async Task<IReadOnlyList<WalletAddressEntry>> GetAllAsync(CancellationToken ct = default)
    {
        var accounts = await _accounts.GetAllAsync(ct);
        return accounts
            .Select(a => new WalletAddressEntry(a.UserId, a.WalletAddress))
            .ToList();
    }
}
