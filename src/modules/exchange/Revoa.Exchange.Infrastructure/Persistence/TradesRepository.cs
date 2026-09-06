using MongoDB.Driver;
using Revoa.Exchange.Domain.Aggregates.TradeAggregate;
using Revoa.Exchange.Domain.Repositories;
using Revoa.Infrastructure.Persistence;

namespace Revoa.Exchange.Infrastructure.Persistence;

public class TradesRepository : MongoRepositoryBase<Trade>, ITradeRepository, IMongoIndexEnsurer
{
    public TradesRepository(IMongoDatabase database) : base(database, "Trades")
    {
    }

    public async Task<IReadOnlyList<Trade>> GetByListingAsync(Guid listingId, CancellationToken ct)
    {
        return await Collection.Find(t => t.ListingId == listingId)
            .SortByDescending(t => t.Version)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Trade>> GetByBuyerAsync(Guid buyerId, int limit, CancellationToken ct)
    {
        var cap = limit > 0 ? limit : 50;
        return await Collection.Find(t => t.BuyerId == buyerId)
            .SortByDescending(t => t.Version)
            .Limit(cap)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Trade>> GetBySellerAsync(Guid sellerId, int limit, CancellationToken ct)
    {
        var cap = limit > 0 ? limit : 50;
        return await Collection.Find(t => t.SellerId == sellerId)
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

        return await Collection.Find(query)
            .SortByDescending(t => t.Version)
            .Skip((safePage - 1) * safeSize)
            .Limit(safeSize)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Cria índices (ListingId, BuyerId, SellerId, State). Idempotente.
    /// </summary>
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await Collection.Indexes.CreateManyAsync(new[]
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
