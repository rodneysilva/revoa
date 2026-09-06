using FluentValidation;

namespace Revoa.Identity.Application.Validators;

public class SetUserRoleCommandValidator : AbstractValidator<Commands.SetUserRoleCommand>
{
    public SetUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId é obrigatório.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Role inválida (use User, Mod, Admin ou Arbitrator).");
    }
}
