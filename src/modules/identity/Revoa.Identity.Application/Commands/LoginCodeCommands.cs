using System.Security.Cryptography;
using MediatR;
using Revoa.Abstractions;
using Revoa.Identity.Application.Services;
using Revoa.Identity.Domain.Aggregates.UserAggregate;
using Revoa.Identity.Domain.Repositories;

namespace Revoa.Identity.Application.Commands;

// Claims suficientes p/ o controller emitir o JWT após login confirmado (sem vazar o aggregate User).
public sealed record LoginClaims(Guid UserId, string Name, string Email, bool EmailVerified, bool PhoneVerified, UserRole Role);

// Etapa 1 do login passwordless: solicita um código de acesso por e-mail.
// Anti-enumeração: sempre Ok (não revela se a conta existe); só envia o código se a conta existir
// e estiver verificada (Active).
public sealed record LoginRequestCommand(string Email) : IRequest<Result>;

public class LoginRequestCommandHandler : IRequestHandler<LoginRequestCommand, Result>
{
    private readonly IUserRepository _users;
    private readonly IEmailSender _emailSender;

    public LoginRequestCommandHandler(IUserRepository users, IEmailSender emailSender)
    {
        _users = users;
        _emailSender = emailSender;
    }

    public async Task<Result> Handle(LoginRequestCommand request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var user = await _users.GetByEmailAsync(request.Email.Trim(), ct);
            if (user is { Status: UserStatus.Active })
            {
                var code = GenerateCode();
                user.SetLoginCode(code, DateTime.UtcNow.AddMinutes(10));
                await _users.UpdateAsync(user, ct);
                await _emailSender.SendLoginCodeAsync(user.Email, code, ct);
            }
        }

        // Mensagem neutra e igual em todos os casos (conta existe/não existe/verificada/não).
        return Result.Ok();
    }

    private static string GenerateCode() => RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
}

// Etapa 2 do login: valida o código de e-mail e devolve as claims p/ emissão do JWT.
public sealed record LoginConfirmCommand(string Email, string Code) : IRequest<Result<LoginClaims>>;

public class LoginConfirmCommandHandler : IRequestHandler<LoginConfirmCommand, Result<LoginClaims>>
{
    private readonly IUserRepository _users;

    public LoginConfirmCommandHandler(IUserRepository users)
    {
        _users = users;
    }

    public async Task<Result<LoginClaims>> Handle(LoginConfirmCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Code))
        {
            return Result<LoginClaims>.Fail("Código inválido ou expirado.");
        }

        var user = await _users.GetByEmailAsync(request.Email.Trim(), ct);
        if (user is null || user.Status != UserStatus.Active || !user.VerifyLoginCode(request.Code.Trim()))
        {
            return Result<LoginClaims>.Fail("Código inválido ou expirado.");
        }

        await _users.UpdateAsync(user, ct);
        return Result<LoginClaims>.Ok(new LoginClaims(user.Id, user.Name, user.Email, user.EmailVerified, user.PhoneVerified, user.Role));
    }
}
