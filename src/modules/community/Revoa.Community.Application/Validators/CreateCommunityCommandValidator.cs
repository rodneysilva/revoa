using FluentValidation;
using Revoa.Community.Domain.Aggregates.CommunityAggregate;

namespace Revoa.Community.Application.Validators;

public class CreateCommunityCommandValidator : AbstractValidator<Commands.CreateCommunityCommand>
{
    public CreateCommunityCommandValidator()
    {
        RuleFor(x => x.Type).IsInEnum().WithMessage("Tipo inválido (Default|User).");
        RuleFor(x => x.Axis).IsInEnum().WithMessage("Eixo inválido (Geo|Interest|Cause).");
        RuleFor(x => x.Visibility).IsInEnum().WithMessage("Visibilidade inválida (Open|Private).");

        RuleFor(x => x.CreatorId).NotEmpty().WithMessage("Criador é obrigatório (claim sub).");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MinimumLength(3).WithMessage("Nome deve ter ao menos 3 caracteres.")
            .MaximumLength(80).WithMessage("Nome deve ter no máximo 80 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Descrição deve ter no máximo 500 caracteres.");

        RuleFor(x => x.CoverImageUrl)
            .MaximumLength(500).WithMessage("URL da capa deve ter no máximo 500 caracteres.");

        // Private exige senha.
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Comunidades privadas exigem senha.")
            .When(x => x.Visibility == CommunityVisibility.Private);
    }
}
