using MongoDB.Driver;
using Revoa.Reputation.Domain.Aggregates.ReviewAggregate;
using Revoa.Reputation.Domain.Repositories;

namespace Revoa.Reputation.Infrastructure.Persistence;

public class ReviewsRepository : IReviewRepository
{
    private readonly IMongoCollection<Review> _reviews;

    public ReviewsRepository(IMongoDatabase database)
    {
        _reviews = database.GetCollection<Review>("Reviews");
    }

    public async Task<Review?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _reviews.Find(r => r.Id == id).FirstOrDefaultAsync(ct);
    }

    public async Task<Review?> GetByTradeAndReviewerAsync(Guid tradeId, Guid reviewerId, CancellationToken ct = default)
    {
        return await _reviews.Find(r => r.TradeId == tradeId && r.ReviewerId == reviewerId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<Review>> GetByRevieweeAsync(Guid revieweeId, int limit, CancellationToken ct = default)
    {
        var cap = limit > 0 ? limit : 20;
        return await _reviews.Find(r => r.RevieweeId == revieweeId)
            .SortByDescending(r => r.CreatedAt)
            .Limit(cap)
            .ToListAsync(ct);
    }

    // Insert (NÃO upsert): a review é imutável. A unicidade TradeId+ReviewerId garante 1 por direção.
    // Version já vem do aggregate Create (=1); bump fica implícito no insert.
    public async Task AddAsync(Review review, CancellationToken ct = default)
    {
        await _reviews.InsertOneAsync(review, cancellationToken: ct);
    }

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await _reviews.Indexes.CreateManyAsync(new[]
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
