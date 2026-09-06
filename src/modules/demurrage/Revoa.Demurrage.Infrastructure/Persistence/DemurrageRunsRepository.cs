using MongoDB.Driver;
using Revoa.Demurrage.Domain.Aggregates.DemurrageRunAggregate;
using Revoa.Demurrage.Domain.Repositories;
using Revoa.Infrastructure.Persistence;

namespace Revoa.Demurrage.Infrastructure.Persistence;

// Append-only: um documento por execução de demurrage (coleção DemurrageRuns). Sem update/optimistic
// locking — o aggregate nasce com Version=1 e nunca muda. Bump de Version é responsabilidade do repo
// (regra do projeto); aqui não há bump porque é insert puro (igual ao AccountsRepository.AddAsync).
public class DemurrageRunsRepository : MongoRepositoryBase<DemurrageRun>, IDemurrageRunRepository, IMongoIndexEnsurer
{
    public DemurrageRunsRepository(IMongoDatabase database) : base(database, "DemurrageRuns")
    {
    }

    public async Task<IReadOnlyList<DemurrageRun>> ListAsync(int limit, CancellationToken ct)
    {
        return await Collection
            .Find(_ => true)
            .SortByDescending(r => r.RunAt)
            .Limit(limit)
            .ToListAsync(ct);
    }

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await Collection.Indexes.CreateOneAsync(
            new CreateIndexModel<DemurrageRun>(
                Builders<DemurrageRun>.IndexKeys.Descending(r => r.RunAt),
                new CreateIndexOptions { Name = "ix_RunAt" }),
            cancellationToken: ct);
    }
}
