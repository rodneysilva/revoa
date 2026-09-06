using MongoDB.Driver;
using Revoa.Reputation.Domain.Repositories;
using Revoa.Infrastructure.Persistence;
using ReputationAggregate = Revoa.Reputation.Domain.Aggregates.ReputationAggregate;

namespace Revoa.Reputation.Infrastructure.Persistence;

public class ReputationsRepository : IReputationRepository, IMongoIndexEnsurer
{
    private readonly IMongoCollection<ReputationAggregate.Reputation> _reputations;

    public ReputationsRepository(IMongoDatabase database)
    {
        _reputations = database.GetCollection<ReputationAggregate.Reputation>("Reputations");
    }

    public async Task<ReputationAggregate.Reputation?> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        return await _reputations.Find(r => r.UserId == userId).FirstOrDefaultAsync(ct);
    }

    // Upsert por UserId (IsUpsert=true): cria se não existe, substitui se existe.
    // Bump de Version é aqui (repositório), nunca no aggregate. Sem optimistic locking —
    // a unicidade de UserId é o controle deste aggregate.
    public async Task UpsertAsync(ReputationAggregate.Reputation reputation, CancellationToken ct)
    {
        reputation.IncrementVersion();

        var filter = Builders<ReputationAggregate.Reputation>.Filter.Eq(r => r.UserId, reputation.UserId);
        await _reputations.ReplaceOneAsync(
            filter, reputation, new ReplaceOptions { IsUpsert = true }, ct);
    }

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await _reputations.Indexes.CreateOneAsync(
            new CreateIndexModel<ReputationAggregate.Reputation>(
                Builders<ReputationAggregate.Reputation>.IndexKeys.Ascending(r => r.UserId),
                new CreateIndexOptions { Name = "ux_UserId", Unique = true }),
            cancellationToken: ct);
    }
}
