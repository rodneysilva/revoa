using Revoa.Reputation.Domain.Aggregates.ReviewAggregate;

namespace Revoa.Reputation.Domain.Repositories;

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(Guid id, CancellationToken ct = default);

    // Anti-duplicata: 1 avaliação por direção (reviewer→reviewee) por trade.
    Task<Review?> GetByTradeAndReviewerAsync(Guid tradeId, Guid reviewerId, CancellationToken ct = default);

    // Avaliações recebidas por um usuário (perfil/ListingDetail), ordenadas por data (desc).
    Task<IReadOnlyList<Review>> GetByRevieweeAsync(Guid revieweeId, int limit, CancellationToken ct = default);

    Task AddAsync(Review review, CancellationToken ct = default);

    Task EnsureIndexesAsync(CancellationToken ct = default);
}
