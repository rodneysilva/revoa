using FluentValidation;

namespace Revoa.Notifications.Application.Validators;

public class SubscribePushCommandValidator : AbstractValidator<Commands.SubscribePushCommand>
{
    public SubscribePushCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Usuário é obrigatório (claim sub).");

        RuleFor(x => x.Endpoint)
            .NotEmpty().WithMessage("Endpoint é obrigatório.")
            .Must(e => e.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Endpoint deve ser uma URL http/https válida.");

        RuleFor(x => x.P256dh).NotEmpty().WithMessage("Chave P256dh é obrigatória.");
        RuleFor(x => x.Auth).NotEmpty().WithMessage("Auth secret é obrigatório.");
    }
}
