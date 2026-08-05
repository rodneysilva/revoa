using Revoa.Coupon.Domain.Aggregates.CouponAggregate;

namespace Revoa.Coupon.Application.DTOs;

// Leitura de um cupom (GET /api/coupons — admin). PascalCase conforme driver Mongo.
public sealed record CouponDto(
    Guid Id,
    string Code,
    long AmountRvm,
    int MaxUses,
    DateTime? Expiry,
    CouponStatus Status,
    string CreatedBy,
    DateTime CreatedAt);

public static class CouponDtoMapper
{
    public static CouponDto From(CouponAggregate c) => new(
        c.Id,
        c.Code,
        c.AmountRvm,
        c.MaxUses,
        c.Expiry,
        c.Status,
        c.CreatedBy,
        c.CreatedAt);
}
