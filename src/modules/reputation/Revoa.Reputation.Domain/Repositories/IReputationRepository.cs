using ReputationAggregate = Revoa.Reputation.Domain.Aggregates.ReputationAggregate;

namespace Revoa.Reputation.Domain.Repositories;

public interface IReputationRepository
{
    Task<ReputationAggregate.Reputation?> GetByUserIdAsync(Guid userId, CancellationToken ct);

    // Upsert por UserId: cria se não existe, substitui se existe (sem optimistic locking —
    // a unicidade de UserId é o controle de concorrência deste aggregate).
    Task UpsertAsync(ReputationAggregate.Reputation reputation, CancellationToken ct);

    Task EnsureIndexesAsync(CancellationToken ct = default);
}
