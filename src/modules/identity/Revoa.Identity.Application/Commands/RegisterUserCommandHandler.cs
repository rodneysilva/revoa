using System.Security.Cryptography;
using MediatR;
using Revoa.Abstractions;
using Revoa.Identity.Application.DTOs;
using Revoa.Identity.Application.Services;
using Revoa.Identity.Domain.Aggregates.UserAggregate;
using Revoa.Identity.Domain.Repositories;
using Revoa.IntegrationContracts.Events;

namespace Revoa.Identity.Application.Commands;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<RegisterUserResult>>
{
    private const string NeutralError = "Não foi possível concluir o cadastro. Verifique seus dados.";

    private readonly IUserRepository _users;
    private readonly IIntegrationEventBus _eventBus;
    private readonly IEmailSender _emailSender;
    private readonly ISmsSender _smsSender;

    public RegisterUserCommandHandler(
        IUserRepository users,
        IIntegrationEventBus eventBus,
        IEmailSender emailSender,
        ISmsSender smsSender)
    {
        _users = users;
        _eventBus = eventBus;
        _emailSender = emailSender;
        _smsSender = smsSender;
    }

    public async Task<Result<RegisterUserResult>> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        // Mensagens específicas e acionáveis (UX) — para uma plataforma comunitária, orientar o
        // usuário vale mais que ofuscar a existência da conta (anti-enumeração). E-mail/telefone únicos.
        if (await _users.GetByEmailAsync(request.Email, ct) is not null)
        {
            return Result<RegisterUserResult>.Fail(
                "Este e-mail já está cadastrado. Faça login ou reenvie a verificação para recuperar seu acesso.");
        }

        if (await _users.GetByPhoneAsync(request.Telefone, ct) is not null)
        {
            return Result<RegisterUserResult>.Fail("Este telefone já está cadastrado em outra conta.");
        }

        var idadeOk = ComputeAge(request.BirthDate) >= 18;
        if (!idadeOk)
        {
            return Result<RegisterUserResult>.Fail("Você precisa ter 18 anos ou mais para se cadastrar.");
        }

        User user;
        try
        {
            user = User.Create(request.Nome, request.Email, request.Telefone, idadeOk);
        }
        catch (DomainException ex)
        {
            return Result<RegisterUserResult>.Fail(ex.Message);
        }

        var now = DateTime.UtcNow;
        var emailToken = GenerateToken();
        var otp = GenerateOtp();
        user.SetEmailVerification(emailToken, now.AddHours(24));
        user.SetPhoneVerification(otp, now.AddMinutes(10));

        try
        {
            await _users.AddAsync(user, ct);
        }
        catch (DuplicateKeyException)
        {
            // Race (TOCTOU) entre o check e o insert: o índice único (Email/Telefone) rejeitou.
            return Result<RegisterUserResult>.Fail("E-mail ou telefone já cadastrado. Tente fazer login.");
        }

        // I/O independente (SMTP + Zenvia): paralelo para reduzir latência do cadastro.
        await Task.WhenAll(
            _emailSender.SendVerificationEmailAsync(user.Email, emailToken, ct),
            _smsSender.SendOtpAsync(user.Telefone, otp, ct));

        await _eventBus.PublishAsync(
            new UserRegisteredEvent(user.Id, user.Email, request.CouponCode),
            ct);

        return Result<RegisterUserResult>.Ok(
            new RegisterUserResult(user.Id, NeedsEmailVerification: true, NeedsPhoneVerification: true));
    }

    private static int ComputeAge(DateOnly birth)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - birth.Year;
        if (birth > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }

    // Token de e-mail: 32 bytes aleatórios (RNG criptográfico), hex.
    private static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }

    // OTP de 6 dígitos (RNG criptográfico).
    private static string GenerateOtp()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }
}
