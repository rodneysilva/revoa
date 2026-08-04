using MongoDB.Driver;
using Revoa.Abstractions;
using Revoa.Exchange.Domain.Aggregates.TradeAggregate;
using Revoa.Exchange.Domain.Repositories;

namespace Revoa.Exchange.Infrastructure.Persistence;

public class TradesRepository : ITradeRepository
{
    private readonly IMongoCollection<Trade> _trades;

    public TradesRepository(IMongoDatabase database)
    {
        _trades = database.GetCollection<Trade>("Trades");
    }

    public async Task<Trade?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _trades.Find(t => t.Id == id).FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<Trade>> GetByListingAsync(Guid listingId, CancellationToken ct)
    {
        return await _trades.Find(t => t.ListingId == listingId)
            .SortByDescending(t => t.Version)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Trade>> GetByBuyerAsync(Guid buyerId, int limit, CancellationToken ct)
    {
        var cap = limit > 0 ? limit : 50;
        return await _trades.Find(t => t.BuyerId == buyerId)
            .SortByDescending(t => t.Version)
            .Limit(cap)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Trade>> GetBySellerAsync(Guid sellerId, int limit, CancellationToken ct)
    {
        var cap = limit > 0 ? limit : 50;
        return await _trades.Find(t => t.SellerId == sellerId)
            .SortByDescending(t => t.Version)
            .Limit(cap)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Trade>> GetHistoryAsync(
        Guid? buyerId, Guid? sellerId, int page, int pageSize, CancellationToken ct)
    {
        var fb = Builders<Trade>.Filter;
        var query = fb.Empty;

        if (buyerId is not null)
        {
            query &= fb.Eq(t => t.BuyerId, buyerId.Value);
        }

        if (sellerId is not null)
        {
            query &= fb.Eq(t => t.SellerId, sellerId.Value);
        }

        var safePage = page <= 0 ? 1 : page;
        var safeSize = pageSize > 0 ? pageSize : 20;

        return await _trades.Find(query)
            .SortByDescending(t => t.Version)
            .Skip((safePage - 1) * safeSize)
            .Limit(safeSize)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Trade trade, CancellationToken ct)
    {
        await _trades.InsertOneAsync(trade, cancellationToken: ct);
    }

    public async Task UpdateAsync(Trade trade, CancellationToken ct)
    {
        var expectedVersion = trade.Version;

        // Optimistic locking: _id + (Version == esperada OU doc legado sem Version).
        var filter = Builders<Trade>.Filter.Eq(t => t.Id, trade.Id)
                     & (Builders<Trade>.Filter.Eq(t => t.Version, expectedVersion)
                        | Builders<Trade>.Filter.Exists(t => t.Version, false));

        trade.IncrementVersion();

        var result = await _trades.ReplaceOneAsync(filter, trade, cancellationToken: ct);
        if (result.MatchedCount == 0)
        {
            throw new ConcurrencyException(trade.Id.ToString(), expectedVersion);
        }
    }

    /// <summary>
    /// Cria índices (ListingId, BuyerId, SellerId, State). Idempotente.
    /// </summary>
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await _trades.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Trade>(
                Builders<Trade>.IndexKeys.Ascending(t => t.ListingId),
                new CreateIndexOptions { Name = "ix_ListingId" }),
            new CreateIndexModel<Trade>(
                Builders<Trade>.IndexKeys.Ascending(t => t.BuyerId),
                new CreateIndexOptions { Name = "ix_BuyerId" }),
            new CreateIndexModel<Trade>(
                Builders<Trade>.IndexKeys.Ascending(t => t.SellerId),
                new CreateIndexOptions { Name = "ix_SellerId" }),
            new CreateIndexModel<Trade>(
                Builders<Trade>.IndexKeys.Ascending(t => t.State),
                new CreateIndexOptions { Name = "ix_State" })
        }, ct);
    }
}
