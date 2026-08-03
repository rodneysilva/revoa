using MediatR;
using Revoa.Abstractions;
using Revoa.Identity.Application.DTOs;
using Revoa.Identity.Application.Services;
using Revoa.Identity.Domain.Aggregates.UserAggregate;
using Revoa.Identity.Domain.Repositories;
using Revoa.IntegrationContracts.Events;

namespace Revoa.Identity.Application.Commands;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
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

    public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        var existing = await _users.GetByEmailAsync(request.Email, ct);
        if (existing is not null)
        {
            throw new InvalidOperationException("E-mail já cadastrado.");
        }

        var idadeOk = ComputeAge(request.BirthDate) >= 18;
        var user = User.Create(request.Nome, request.Email, request.Telefone, idadeOk);
        await _users.AddAsync(user, ct);

        var emailToken = Guid.NewGuid().ToString("N");
        await _emailSender.SendVerificationEmailAsync(user.Email, emailToken, ct);
        await _smsSender.SendOtpAsync(user.Telefone, GenerateOtp(), ct);

        await _eventBus.PublishAsync(
            new UserRegisteredEvent(user.Id, user.Email, request.CouponCode),
            ct);

        return new RegisterUserResult(user.Id, NeedsEmailVerification: true, NeedsPhoneVerification: true);
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

    private static string GenerateOtp() => Random.Shared.Next(100000, 999999).ToString();
}
