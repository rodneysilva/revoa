using Revoa.Coupon.Domain.Aggregates.CouponAggregate;

namespace Revoa.Coupon.Domain.Repositories;

public interface ICouponRepository
{
    Task<CouponAggregate?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<CouponAggregate?> GetByCodeAsync(string code, CancellationToken ct = default);

    Task<IReadOnlyList<CouponAggregate>> ListAsync(int page, int pageSize, CancellationToken ct = default);

    Task AddAsync(CouponAggregate coupon, CancellationToken ct = default);

    Task UpdateAsync(CouponAggregate coupon, CancellationToken ct = default);

    Task EnsureIndexesAsync(CancellationToken ct = default);
}
