using Revoa.Exchange.Domain.Aggregates.TradeAggregate;

namespace Revoa.Exchange.Domain.Repositories;

public interface ITradeRepository
{
    Task<Trade?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Trade>> GetByListingAsync(Guid listingId, CancellationToken ct = default);

    Task<IReadOnlyList<Trade>> GetByBuyerAsync(Guid buyerId, int limit, CancellationToken ct = default);

    Task<IReadOnlyList<Trade>> GetBySellerAsync(Guid sellerId, int limit, CancellationToken ct = default);

    // Histórico paginado por comprador e/ou vendedor (ambos null → todos).
    Task<IReadOnlyList<Trade>> GetHistoryAsync(
        Guid? buyerId,
        Guid? sellerId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task AddAsync(Trade trade, CancellationToken ct = default);

    Task UpdateAsync(Trade trade, CancellationToken ct = default);

    Task EnsureIndexesAsync(CancellationToken ct = default);
}
