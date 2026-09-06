using FluentValidation;

namespace Revoa.Identity.Application.Validators;

public class RegisterUserCommandValidator : AbstractValidator<Commands.RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MinimumLength(2).WithMessage("Nome deve ter ao menos 2 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .EmailAddress().WithMessage("E-mail inválido.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Telefone é obrigatório.");

        RuleFor(x => x.BirthDate)
            .Must(BeAtLeast18).WithMessage("Idade mínima de 18 anos.");
    }

    private static bool BeAtLeast18(DateOnly birth)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - birth.Year;
        if (birth > today.AddYears(-age))
        {
            age--;
        }

        return age >= 18;
    }
}
