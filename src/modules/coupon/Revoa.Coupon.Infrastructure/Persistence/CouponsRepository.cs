using MongoDB.Driver;
using Revoa.Abstractions;
using Revoa.Coupon.Domain.Aggregates.CouponAggregate;
using Revoa.Coupon.Domain.Repositories;

namespace Revoa.Coupon.Infrastructure.Persistence;

public class CouponsRepository : ICouponRepository
{
    private readonly IMongoCollection<CouponAggregate> _coupons;

    public CouponsRepository(IMongoDatabase database)
    {
        _coupons = database.GetCollection<CouponAggregate>("Coupons");
    }

    public async Task<CouponAggregate?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _coupons.Find(c => c.Id == id).FirstOrDefaultAsync(ct);
    }

    public async Task<CouponAggregate?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        return await _coupons.Find(c => c.Code == code).FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<CouponAggregate>> ListAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var safePage = page > 0 ? page : 1;
        var safeSize = pageSize > 0 ? pageSize : 50;

        return await _coupons.Find(Builders<CouponAggregate>.Filter.Empty)
            .SortByDescending(c => c.CreatedAt)
            .Skip((safePage - 1) * safeSize)
            .Limit(safeSize)
            .ToListAsync(ct);
    }

    public async Task AddAsync(CouponAggregate coupon, CancellationToken ct = default)
    {
        await _coupons.InsertOneAsync(coupon, cancellationToken: ct);
    }

    // Optimistic locking: _id + (Version == esperada OU doc legado sem Version). Bump de Version é AQUI
    // (repositório), nunca no mutator Revoke() do aggregate. MatchedCount==0 → ConcurrencyException.
    public async Task UpdateAsync(CouponAggregate coupon, CancellationToken ct = default)
    {
        var expectedVersion = coupon.Version;

        var filter = Builders<CouponAggregate>.Filter.Eq(c => c.Id, coupon.Id)
                     & (Builders<CouponAggregate>.Filter.Eq(c => c.Version, expectedVersion)
                        | Builders<CouponAggregate>.Filter.Exists(c => c.Version, false));

        coupon.IncrementVersion();

        var result = await _coupons.ReplaceOneAsync(filter, coupon, cancellationToken: ct);
        if (result.MatchedCount == 0)
        {
            throw new ConcurrencyException(coupon.Id.ToString(), expectedVersion);
        }
    }

    // ix_Code (busca por código; correlação on/off-chain) + ix_Status_CreatedAt (painel admin: mais
    // recentes primeiro). Idempotente.
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await _coupons.Indexes.CreateManyAsync(new[]
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
