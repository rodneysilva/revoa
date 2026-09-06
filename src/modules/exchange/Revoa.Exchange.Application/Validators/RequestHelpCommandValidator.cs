using FluentValidation;
using Revoa.Exchange.Application.Commands;

namespace Revoa.Exchange.Application.Validators;

public class RequestHelpCommandValidator : AbstractValidator<RequestHelpCommand>
{
    public RequestHelpCommandValidator()
    {
        RuleFor(x => x.ListingId).NotEmpty().WithMessage("Anúncio é obrigatório.");
        RuleFor(x => x.AuthorId).NotEmpty().WithMessage("Autor é obrigatório (claim sub).");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Mensagem é obrigatória.")
            .MinimumLength(1).WithMessage("Mensagem é obrigatória.")
            .MaximumLength(500).WithMessage("Mensagem deve ter no máximo 500 caracteres.");
    }
}
