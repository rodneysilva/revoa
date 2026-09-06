using System.Security.Cryptography;
using MediatR;
using Revoa.Abstractions;
using Revoa.Identity.Application.DTOs;
using Revoa.Identity.Application.Services;
using Revoa.Identity.Domain.Aggregates.UserAggregate;
using Revoa.Identity.Domain.Repositories;

namespace Revoa.Identity.Application.Commands;

// Recuperação de acesso: reenvia token de e-mail + OTP para uma conta ainda não verificada.
// (Não há senha — a autenticação é passwordless/passkey; "recuperar" = concluir a verificação.)
public sealed record ResendVerificationCommand(string Email) : IRequest<Result<RegisterUserResult>>;

public class ResendVerificationCommandHandler : IRequestHandler<ResendVerificationCommand, Result<RegisterUserResult>>
{
    private readonly IUserRepository _users;
    private readonly IEmailSender _emailSender;
    private readonly ISmsSender _smsSender;
    private readonly IOtpHasher _otpHasher;

    public ResendVerificationCommandHandler(IUserRepository users, IEmailSender emailSender, ISmsSender smsSender, IOtpHasher otpHasher)
    {
        _users = users;
        _emailSender = emailSender;
        _smsSender = smsSender;
        _otpHasher = otpHasher;
    }

    public async Task<Result<RegisterUserResult>> Handle(ResendVerificationCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Result<RegisterUserResult>.Fail("E-mail é obrigatório.");
        }

        var user = await _users.GetByEmailAsync(request.Email.Trim(), ct);
        if (user is null)
        {
            return Result<RegisterUserResult>.Fail("Nenhuma conta encontrada com este e-mail. Cadastre-se primeiro.");
        }

        if (user.Status == UserStatus.Active)
        {
            return Result<RegisterUserResult>.Fail("Sua conta já está verificada. Faça login normalmente.");
        }

        // Gera novos códigos (os anteriores expiram/invalidam).
        var now = DateTime.UtcNow;
        var emailToken = GenerateToken();
        var otp = GenerateOtp();
        user.SetEmailVerification(emailToken, now.AddHours(24));
        user.SetPhoneVerification(_otpHasher.Hash(otp), now.AddMinutes(10));
        await _users.UpdateAsync(user, ct);

        await Task.WhenAll(
            _emailSender.SendVerificationEmailAsync(user.Email, emailToken, ct),
            _smsSender.SendOtpAsync(user.Phone, otp, ct));

        return Result<RegisterUserResult>.Ok(
            new RegisterUserResult(user.Id, NeedsEmailVerification: true, NeedsPhoneVerification: true));
    }

    private static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }

    private static string GenerateOtp() => RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
}
