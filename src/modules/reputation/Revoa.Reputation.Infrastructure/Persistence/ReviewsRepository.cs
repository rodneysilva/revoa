using MongoDB.Driver;
using Revoa.Reputation.Domain.Aggregates.ReviewAggregate;
using Revoa.Reputation.Domain.Repositories;
using Revoa.Infrastructure.Persistence;

namespace Revoa.Reputation.Infrastructure.Persistence;

public class ReviewsRepository : MongoRepositoryBase<Review>, IReviewRepository, IMongoIndexEnsurer
{
    public ReviewsRepository(IMongoDatabase database) : base(database, "Reviews")
    {
    }

    public async Task<Review?> GetByTradeAndReviewerAsync(Guid tradeId, Guid reviewerId, CancellationToken ct = default)
    {
        return await Collection.Find(r => r.TradeId == tradeId && r.ReviewerId == reviewerId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<Review>> GetByRevieweeAsync(Guid revieweeId, int limit, CancellationToken ct = default)
    {
        var cap = limit > 0 ? limit : 20;
        return await Collection.Find(r => r.RevieweeId == revieweeId)
            .SortByDescending(r => r.CreatedAt)
            .Limit(cap)
            .ToListAsync(ct);
    }

    // Insert (NÃO upsert): a review é imutável. A unicidade TradeId+ReviewerId garante 1 por direção.
    // Version já vem do aggregate Create (=1); bump fica implícito no insert.
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await Collection.Indexes.CreateManyAsync(new[]
        {
            // 1 avaliação por direção (reviewer) por trade — anti-duplicata em nível de banco.
            new CreateIndexModel<Review>(
                Builders<Review>.IndexKeys
                    .Ascending(r => r.TradeId)
                    .Ascending(r => r.ReviewerId),
                new CreateIndexOptions { Name = "ux_Trade_Reviewer", Unique = true }),
            // Consulta de avaliações recebidas por usuário (perfil), ordenada por data.
            new CreateIndexModel<Review>(
                Builders<Review>.IndexKeys
                    .Ascending(r => r.RevieweeId)
                    .Descending(r => r.CreatedAt),
                new CreateIndexOptions { Name = "ix_Reviewee_CreatedAt" })
        }, ct);
    }
}
