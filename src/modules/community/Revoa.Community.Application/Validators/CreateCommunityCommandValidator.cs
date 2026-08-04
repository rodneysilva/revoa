using FluentValidation;
using Revoa.Community.Domain.Aggregates.CommunityAggregate;

namespace Revoa.Community.Application.Validators;

public class CreateCommunityCommandValidator : AbstractValidator<Commands.CreateCommunityCommand>
{
    public CreateCommunityCommandValidator()
    {
        RuleFor(x => x.Tipo).IsInEnum().WithMessage("Tipo inválido (Default|User).");
        RuleFor(x => x.Eixo).IsInEnum().WithMessage("Eixo inválido (Geo|Interesse|Causa).");
        RuleFor(x => x.Visibilidade).IsInEnum().WithMessage("Visibilidade inválida (Open|Private).");

        RuleFor(x => x.CriadorId).NotEmpty().WithMessage("Criador é obrigatório (claim sub).");

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MinimumLength(3).WithMessage("Nome deve ter ao menos 3 caracteres.")
            .MaximumLength(80).WithMessage("Nome deve ter no máximo 80 caracteres.");

        RuleFor(x => x.Descricao)
            .MaximumLength(500).WithMessage("Descrição deve ter no máximo 500 caracteres.");

        // Private exige senha.
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Comunidades privadas exigem senha.")
            .When(x => x.Visibilidade == CommunityVisibilidade.Private);
    }
}
