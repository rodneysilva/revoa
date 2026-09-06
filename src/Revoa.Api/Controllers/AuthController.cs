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

    // Login passwordless (produção): etapa 1 — solicita código de acesso por e-mail.
    // Resposta neutra (anti-enumeração): não revela se a conta existe.
    [HttpPost("login/request")]
    [AllowAnonymous]
    public async Task<ActionResult> LoginRequest([FromBody] ResendRequest request, CancellationToken ct)
    {
        await _mediator.Send(new LoginRequestCommand(request.Email), ct);
        return Ok(new { message = "Se a conta existir, enviamos um código para o e-mail informado." });
    }

    // Login passwordless (produção): etapa 2 — valida o código e emite o JWT.
    [HttpPost("login/confirm")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResult>> LoginConfirm([FromBody] LoginConfirmRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new LoginConfirmCommand(request.Email, request.Code), ct);
        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        var c = result.Value;
        var roles = RolesFor(c.Email, c.Role);
        var token = IssueJwt(c.UserId, c.Nome, c.Email, c.EmailVerified, c.PhoneVerified, roles);
        return Ok(new LoginResult(token, c.UserId, c.Nome, c.Email, roles));
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

        var roles = RolesFor(user.Email, user.Role);
        var token = IssueJwt(user.Id, user.Nome, user.Email, user.EmailVerified, user.PhoneVerified, roles);
        return Ok(new LoginResult(token, user.Id, user.Nome, user.Email, roles));
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

        var roles = RolesFor(user.Email, user.Role);
        var token = IssueJwt(user.Id, user.Nome, user.Email, user.EmailVerified, user.PhoneVerified, roles);
        return Ok(new LoginResult(token, user.Id, user.Nome, user.Email, roles));
    }

    // Roles do JWT: role do aggregate + "Admin" quando o e-mail está no allowlist Admin:Emails.
    // A policy "Arbitrator" aceita RequireRole("Arbitrator", "Admin") — admins são árbitros por padrão.
    private string[] RolesFor(string email, UserRole role)
    {
        var roles = new List<string> { role.ToString() };
        var adminEmails = _config.GetSection("Admin:Emails").Get<string[]>() ?? Array.Empty<string>();
        if (adminEmails.Contains(email, StringComparer.OrdinalIgnoreCase))
        {
            roles.Add("Admin");
        }

        return roles.Distinct().ToArray();
    }

    // JWT com as claims que a policy "Verified" exige (email_verified + phone_verified) e as
    // claims "role" que alimentam as policies baseadas em role (Arbitrator).
    private string IssueJwt(
        Guid userId, string name, string email, bool emailVerified, bool phoneVerified, string[] roles)
    {
        // Sem fallback hardcoded: o fail-fast do Program.cs (startup) garante a chave; se chegou
        // aqui sem Jwt:Key, é bug de configuração — falha explícita em vez de assinar com default.
        var key = _config["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key ausente (fail-fast do Program.cs deveria ter capturado).");
        var issuer = _config["Jwt:Issuer"] ?? "revoa";
        var audience = _config["Jwt:Audience"] ?? "revoa";

        var claims = new List<Claim>
        {
            new("sub", userId.ToString()),
            new("name", name),
            new("email", email),
            new("email_verified", emailVerified.ToString().ToLowerInvariant()),
            new("phone_verified", phoneVerified.ToString().ToLowerInvariant()),
        };
        claims.AddRange(roles.Select(r => new Claim("role", r)));

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

public sealed record LoginConfirmRequest(string Email, string Code);

public sealed record ResendRequest(string Email);

public sealed record LoginResult(string Token, Guid UserId, string Nome, string Email, string[] Roles);
