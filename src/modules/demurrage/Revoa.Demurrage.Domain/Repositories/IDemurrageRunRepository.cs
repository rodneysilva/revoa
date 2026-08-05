using Revoa.Demurrage.Domain.Aggregates.DemurrageRunAggregate;

namespace Revoa.Demurrage.Domain.Repositories;

public interface IDemurrageRunRepository
{
    Task AddAsync(DemurrageRun run, CancellationToken ct);

    // Histórico ordenado por RunAt desc. Append-only (sem update/optimistic locking).
    Task<IReadOnlyList<DemurrageRun>> ListAsync(int limit, CancellationToken ct);

    Task EnsureIndexesAsync(CancellationToken ct = default);
}
