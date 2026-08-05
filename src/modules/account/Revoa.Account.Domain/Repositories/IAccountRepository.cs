using Revoa.Account.Domain.Aggregates.AccountAggregate;

namespace Revoa.Account.Domain.Repositories;

public interface IAccountRepository
{
    Task<UserAccount?> GetByUserIdAsync(Guid userId, CancellationToken ct);

    Task AddAsync(UserAccount wallet, CancellationToken ct);

    Task UpdateAsync(UserAccount wallet, CancellationToken ct);

    // Todas as carteiras (leitura p/ portas de integração — ex.: Demurrage via IWalletAddressReader).
    Task<IReadOnlyList<UserAccount>> GetAllAsync(CancellationToken ct);
}
