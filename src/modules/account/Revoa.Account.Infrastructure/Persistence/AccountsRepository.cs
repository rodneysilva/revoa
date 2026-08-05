using MongoDB.Driver;
using Revoa.Abstractions;
using Revoa.Account.Domain.Aggregates.AccountAggregate;
using Revoa.Account.Domain.Repositories;
namespace Revoa.Account.Infrastructure.Persistence;

public class AccountsRepository : IAccountRepository
{
    private readonly IMongoCollection<UserAccount> _accounts;

    public AccountsRepository(IMongoDatabase database)
    {
        _accounts = database.GetCollection<UserAccount>("Accounts");
    }

    public async Task<UserAccount?> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        return await _accounts.Find(a => a.UserId == userId).FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<UserAccount>> GetAllAsync(CancellationToken ct)
    {
        return await _accounts.Find(_ => true).ToListAsync(ct);
    }

    public async Task AddAsync(UserAccount wallet, CancellationToken ct)
    {
        await _accounts.InsertOneAsync(wallet, cancellationToken: ct);
    }

    public async Task UpdateAsync(UserAccount wallet, CancellationToken ct)
    {
        var expectedVersion = wallet.Version;

        // Optimistic locking: _id + (Version == esperada OU doc legado sem Version)
        var filter = Builders<UserAccount>.Filter.Eq(a => a.Id, wallet.Id)
                     & (Builders<UserAccount>.Filter.Eq(a => a.Version, expectedVersion)
                        | Builders<UserAccount>.Filter.Exists(a => a.Version, false));

        wallet.IncrementVersion();

        var result = await _accounts.ReplaceOneAsync(filter, wallet, cancellationToken: ct);
        if (result.MatchedCount == 0)
        {
            throw new ConcurrencyException(wallet.Id.ToString(), expectedVersion);
        }
    }

    /// <summary>
    /// Cria índice único em UserId (1 carteira por usuário no MVP dev). Idempotente.
    /// </summary>
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        var keys = Builders<UserAccount>.IndexKeys.Ascending(a => a.UserId);
        await _accounts.Indexes.CreateOneAsync(
            new CreateIndexModel<UserAccount>(keys, new CreateIndexOptions { Name = "ux_UserId", Unique = true }),
            cancellationToken: ct);
    }
}
