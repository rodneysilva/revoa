using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Revoa.Abstractions;
using Revoa.Identity.Application.Commands;
using Revoa.Identity.Application.DTOs;
using Revoa.Identity.Domain.Aggregates.UserAggregate;
using Revoa.Identity.Domain.Repositories;

namespace Revoa.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserRepository _users;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public AuthController(
        IMediator mediator,
        IUserRepository users,
        IConfiguration config,
        IWebHostEnvironment env)
    {
        _mediator = mediator;
        _users = users;
        _config = config;
        _env = env;
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

    // Recuperação de acesso: reenvia token de e-mail + OTP p/ conta ainda não verificada.
    // (Passwordless: não há senha — recuperar = concluir a verificação.)
    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<ActionResult<RegisterUserResult>> ResendVerification(
        [FromBody] ResendRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ResendVerificationCommand(request.Email), ct);
        return result.IsFailure ? BadRequest(new { error = result.Error }) : Ok(result.Value);
    }

    // DEV-ONLY: emite JWT para um usuário já verificado (Status=Active). A auth real é passkey/AA
    // (carteira invisível); este atalho existe só para destravar o desenvolvimento/teste do frontend.
    // Em produção retorna 404. Não exige senha (não há) — confia no estado verificado do usuário.
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResult>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { error = "E-mail é obrigatório." });
        }

        var user = await _users.GetByEmailAsync(request.Email.Trim(), ct);
        if (user is null || user.Status != UserStatus.Active)
        {
            return BadRequest(new { error = "Usuário não encontrado ou não verificado." });
        }

        var token = IssueDevJwt(user);
        return Ok(new LoginResult(token, user.Id, user.Nome, user.Email));
    }

    // DEV-ONLY: verifica e-mail + telefone e ativa o usuário automaticamente (Status=Active).
    // Em dev o Postfix/Zenvia não entregam em caixa real — este atalho destrava o cadastro sem
    // precisar do token/OTP reais. Em produção retorna 404. Não valida o token (confia no e-mail).
    [HttpPost("dev-verify")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResult>> DevVerify([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { error = "E-mail é obrigatório." });
        }

        var user = await _users.GetByEmailAsync(request.Email.Trim(), ct);
        if (user is null)
        {
            return BadRequest(new { error = "Usuário não encontrado. Cadastre-se primeiro." });
        }

        user.DevActivate();
        await _users.UpdateAsync(user, ct);

        var token = IssueDevJwt(user);
        return Ok(new LoginResult(token, user.Id, user.Nome, user.Email));
    }

    // JWT dev com as claims que a policy "Verified" exige (email_verified + phone_verified).
    private string IssueDevJwt(User user)
    {
        var key = _config["Jwt:Key"] ?? "revoa-dev-key-do-not-use-in-prod-min-32-chars!!";
        var issuer = _config["Jwt:Issuer"] ?? "revoa";
        var audience = _config["Jwt:Audience"] ?? "revoa";

        var claims = new[]
        {
            new Claim("sub", user.Id.ToString()),
            new Claim("name", user.Nome),
            new Claim("email", user.Email),
            new Claim("email_verified", user.EmailVerified.ToString().ToLowerInvariant()),
            new Claim("phone_verified", user.PhoneVerified.ToString().ToLowerInvariant()),
        };

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
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

public sealed record LoginRequest(string Email);

public sealed record ResendRequest(string Email);

public sealed record LoginResult(string Token, Guid UserId, string Nome, string Email);
