using MongoDB.Driver;
using Revoa.Coupon.Domain.Aggregates.CouponAggregate;
using Revoa.Coupon.Domain.Repositories;
using Revoa.Infrastructure.Persistence;

namespace Revoa.Coupon.Infrastructure.Persistence;

public class CouponsRepository : MongoRepositoryBase<CouponAggregate>, ICouponRepository, IMongoIndexEnsurer
{
    public CouponsRepository(IMongoDatabase database) : base(database, "Coupons")
    {
    }

    public async Task<CouponAggregate?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        return await Collection.Find(c => c.Code == code).FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<CouponAggregate>> ListAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var safePage = page > 0 ? page : 1;
        var safeSize = pageSize > 0 ? pageSize : 50;

        return await Collection.Find(Builders<CouponAggregate>.Filter.Empty)
            .SortByDescending(c => c.CreatedAt)
            .Skip((safePage - 1) * safeSize)
            .Limit(safeSize)
            .ToListAsync(ct);
    }

    // ix_Code (busca por código; correlação on/off-chain) + ix_Status_CreatedAt (painel admin: mais
    // recentes primeiro). Idempotente.
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await Collection.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<CouponAggregate>(
                Builders<CouponAggregate>.IndexKeys.Ascending(c => c.Code),
                new CreateIndexOptions { Name = "ix_Code" }),
            new CreateIndexModel<CouponAggregate>(
                Builders<CouponAggregate>.IndexKeys
                    .Ascending(c => c.Status)
                    .Descending(c => c.CreatedAt),
                new CreateIndexOptions { Name = "ix_Status_CreatedAt" })
        }, ct);
    }
}
