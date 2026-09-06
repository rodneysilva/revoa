using MongoDB.Driver;
using Revoa.Account.Domain.Aggregates.AccountAggregate;
using Revoa.Account.Domain.Repositories;
using Revoa.Infrastructure.Persistence;

namespace Revoa.Account.Infrastructure.Persistence;

public class AccountsRepository : MongoRepositoryBase<UserAccount>, IAccountRepository, IMongoIndexEnsurer
{
    public AccountsRepository(IMongoDatabase database) : base(database, "Accounts")
    {
    }

    public async Task<UserAccount?> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        return await Collection.Find(a => a.UserId == userId).FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<UserAccount>> GetAllAsync(CancellationToken ct)
    {
        return await Collection.Find(_ => true).ToListAsync(ct);
    }

    /// <summary>
    /// Cria índice único em UserId (1 carteira por usuário no MVP dev). Idempotente.
    /// </summary>
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        var keys = Builders<UserAccount>.IndexKeys.Ascending(a => a.UserId);
        await Collection.Indexes.CreateOneAsync(
            new CreateIndexModel<UserAccount>(keys, new CreateIndexOptions { Name = "ux_UserId", Unique = true }),
            cancellationToken: ct);
    }
}
