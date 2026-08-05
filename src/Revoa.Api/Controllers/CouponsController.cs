using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Revoa.Coupon.Application.Commands;
using Revoa.Coupon.Application.DTOs;
using Revoa.Coupon.Application.Queries;

namespace Revoa.Api.Controllers;

// Cupom on-chain (UF-29): admin cria/revoga/lista cupons (Policy Admin); usuário verificado resgata
// (Policy Verified) — o RVM é mintado on-chain na carteira do usuário. Criar/revogar/listar exigem admin;
// resgatar exige login+verificação. createdBy/userId vêm do token (claims), nunca do body.
[ApiController]
[Route("api/coupons")]
public class CouponsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CouponsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Cria cupom (amount/maxUses/expiry opcional/code opcional → admin compartilha o code gerado).
    // Gate Admin. createdBy = e-mail do admin (claim email).
    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<CouponDto>> Create([FromBody] CreateCouponRequest request, CancellationToken ct)
    {
        var createdBy = User.FindFirst("email")?.Value
                         ?? User.FindFirst("name")?.Value
                         ?? "Admin";

        DateTime? expiry = null;
        if (!string.IsNullOrWhiteSpace(request.Expiry) && DateTime.TryParse(request.Expiry, out var parsed))
        {
            expiry = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }

        var result = await _mediator.Send(
            new CreateCouponCommand(request.AmountRvm, request.MaxUses, expiry, request.Code, createdBy), ct);

        return result.IsFailure
            ? BadRequest(new { error = result.Error })
            : Ok(result.Value);
    }

    // Lista cupons para o painel admin (mais recentes primeiro). Gate Admin.
    [HttpGet]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<IReadOnlyList<CouponDto>>> List(
        [FromQuery] int page = 1, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListCouponsQuery(page), ct);
        return result.IsFailure ? BadRequest(new { error = result.Error }) : Ok(result.Value);
    }

    // Revoga cupom (invalida o resgate on-chain). Gate Admin.
    [HttpPost("{id:guid}/revoke")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult> Revoke(Guid id, CancellationToken ct)
    {
        var by = User.FindFirst("email")?.Value
                 ?? User.FindFirst("name")?.Value
                 ?? "Admin";

        var result = await _mediator.Send(new RevokeCouponCommand(id, by), ct);
        return result.IsFailure ? BadRequest(new { error = result.Error }) : Ok();
    }

    // Resgata cupom (mint on-chain de RVM na carteira do usuário). Gate Verified. userId do token (claim sub).
    [HttpPost("redeem")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult> Redeem([FromBody] RedeemCouponRequest request, CancellationToken ct)
    {
        var sub = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var userId))
        {
            return Unauthorized(new { error = "Token sem claim 'sub'." });
        }

        var result = await _mediator.Send(new RedeemCouponCommand(userId, request.Code ?? string.Empty), ct);
        return result.IsFailure ? BadRequest(new { error = result.Error }) : Ok();
    }
}

public sealed record CreateCouponRequest(long AmountRvm, int MaxUses, string? Expiry, string? Code);
public sealed record RedeemCouponRequest(string? Code);
