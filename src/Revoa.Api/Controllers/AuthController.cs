using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Revoa.Abstractions;
using Revoa.Identity.Application.Commands;
using Revoa.Identity.Application.DTOs;

namespace Revoa.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<RegisterUserResult>> Register(
        [FromBody] RegisterUserRequest request, CancellationToken ct)
    {
        var command = new RegisterUserCommand(
            request.Nome, request.Email, request.Telefone, request.BirthDate, request.CouponCode);

        var result = await _mediator.Send(command, ct);
        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<ActionResult> VerifyEmail([FromBody] VerifyTokenRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new VerifyEmailCommand(request.UserId, request.Token), ct);
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    [HttpPost("verify-phone")]
    [AllowAnonymous]
    public async Task<ActionResult> VerifyPhone([FromBody] VerifyPhoneRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new VerifyPhoneCommand(request.UserId, request.Code), ct);
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }
}

public sealed record RegisterUserRequest(
    string Nome,
    string Email,
    string Telefone,
    DateOnly BirthDate,
    string? CouponCode);

public sealed record VerifyTokenRequest(Guid UserId, string Token);

public sealed record VerifyPhoneRequest(Guid UserId, string Code);
