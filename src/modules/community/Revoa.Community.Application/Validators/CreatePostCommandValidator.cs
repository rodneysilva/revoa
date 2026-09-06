using FluentValidation;

namespace Revoa.Community.Application.Validators;

public class CreatePostCommandValidator : AbstractValidator<Commands.CreatePostCommand>
{
    public CreatePostCommandValidator()
    {
        RuleFor(x => x.AutorId).NotEmpty().WithMessage("Autor é obrigatório (claim sub).");
        RuleFor(x => x.CommunityId).NotEmpty().WithMessage("Comunidade é obrigatória.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Conteúdo é obrigatório.")
            .MaximumLength(2000).WithMessage("Conteúdo deve ter no máximo 2000 caracteres.");
    }
}
