using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Revoa.Abstractions;
using Revoa.Token.Application.Queries;

namespace Revoa.Api.Controllers;

// Carteira do usuário logado. O saldo RVM é leitura on-chain (balanceOf) — degrada
// para Rvm = null quando a chain está inacessível, sem falhar o request.
[ApiController]
[Route("api/wallet")]
public class WalletController : ControllerBase
{
    private readonly IMediator _mediator;

    public WalletController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Saldo do próprio usuário (VISUAL_IDENTITY §8 — chip "RM$" no header).
    [HttpGet("balance")]
    [Authorize]
    public async Task<ActionResult<WalletBalanceDto>> Balance(CancellationToken ct)
    {
        var user = User.GetRevoaUser();
        if (user is null) return Unauthorized(new ApiError("Token sem claim 'sub'."));

        var result = await _mediator.Send(new GetMyBalanceQuery(user.UserId), ct);
        return Ok(result.Value);
    }
}
