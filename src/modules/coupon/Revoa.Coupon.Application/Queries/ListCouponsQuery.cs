using MediatR;
using Revoa.Abstractions;
using Revoa.Coupon.Application.DTOs;
using Revoa.Coupon.Domain.Repositories;

namespace Revoa.Coupon.Application.Queries;

// Lista cupons para o painel admin (UF-29), paginados (mais recentes primeiro). Gate Admin no controller.
public sealed record ListCouponsQuery(int Page) : IRequest<Result<IReadOnlyList<CouponDto>>>;

public class ListCouponsQueryHandler : IRequestHandler<ListCouponsQuery, Result<IReadOnlyList<CouponDto>>>
{
    private const int PageSize = 50;
    private readonly ICouponRepository _coupons;

    public ListCouponsQueryHandler(ICouponRepository coupons)
    {
        _coupons = coupons;
    }

    public async Task<Result<IReadOnlyList<CouponDto>>> Handle(ListCouponsQuery request, CancellationToken ct)
    {
        var page = request.Page > 0 ? request.Page : 1;
        var coupons = await _coupons.ListAsync(page, PageSize, ct);
        var dtos = coupons.Select(CouponDtoMapper.From).ToList();
        return Result<IReadOnlyList<CouponDto>>.Ok(dtos);
    }
}
