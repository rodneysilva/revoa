using MediatR;
using Revoa.Abstractions;
using Revoa.Coupon.Application.Services;
using Revoa.Coupon.Domain.Aggregates.CouponAggregate;
using Revoa.Coupon.Domain.Repositories;

namespace Revoa.Coupon.Application.Commands;

// Revoga cupom (UF-29): admin invalida o resgate on-chain e marca o doc off-chain como Revoked.
// Gate Admin no controller. Bump de Version é no repositório (optimistic locking).
public sealed record RevokeCouponCommand(Guid CouponId, string By) : IRequest<Result>;

public class RevokeCouponCommandHandler : IRequestHandler<RevokeCouponCommand, Result>
{
    private readonly ICouponRepository _coupons;
    private readonly ICouponChainService _chain;

    public RevokeCouponCommandHandler(ICouponRepository coupons, ICouponChainService chain)
    {
        _coupons = coupons;
        _chain = chain;
    }

    public async Task<Result> Handle(RevokeCouponCommand request, CancellationToken ct)
    {
        var coupon = await _coupons.GetByIdAsync(request.CouponId, ct);
        if (coupon is null)
        {
            return Result.Fail("Cupom não encontrado.");
        }

        if (coupon.Status == CouponStatus.Revoked)
        {
            return Result.Fail("Cupom já está revogado.");
        }

        try
        {
            await _chain.RevokeCouponAsync(coupon.Code, ct);
            coupon.Revoke();
            await _coupons.UpdateAsync(coupon, ct);

            return Result.Ok();
        }
        catch (CouponChainException ex)
        {
            return Result.Fail("Falha ao revogar cupom: " + ex.Message);
        }
        catch (DomainException ex)
        {
            return Result.Fail(ex.Message);
        }
    }
}
