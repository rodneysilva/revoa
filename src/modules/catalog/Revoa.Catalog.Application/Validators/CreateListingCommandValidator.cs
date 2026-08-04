using FluentValidation;
using Revoa.Catalog.Domain.Aggregates.ListingAggregate;

namespace Revoa.Catalog.Application.Validators;

public class CreateListingCommandValidator : AbstractValidator<Commands.CreateListingCommand>
{
    public CreateListingCommandValidator()
    {
        RuleFor(x => x.Kind).IsInEnum().WithMessage("Kind inválido (Product|Service).");
        RuleFor(x => x.Modo).IsInEnum().WithMessage("Modo inválido (Trocar|Repassar|Doar|Voluntariar).");
        RuleFor(x => x.Visibilidade).IsInEnum().WithMessage("Visibilidade inválida (Comunidade|Global|Ambos).");

        RuleFor(x => x.Titulo)
            .NotEmpty().WithMessage("Título é obrigatório.")
            .MinimumLength(3).WithMessage("Título deve ter ao menos 3 caracteres.")
            .MaximumLength(120).WithMessage("Título deve ter no máximo 120 caracteres.");

        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("Descrição é obrigatória.")
            .MaximumLength(2000).WithMessage("Descrição deve ter no máximo 2000 caracteres.");

        RuleFor(x => x.PrecoRvm)
            .GreaterThanOrEqualTo(0).WithMessage("Preço RVM deve ser ≥ 0.");

        // Doar/Voluntariar = 0 RVM.
        RuleFor(x => x.PrecoRvm)
            .Equal(0).WithMessage("Doar/voluntariar deve ter preço 0 RVM.")
            .When(x => x.Modo is ListingModo.Doar or ListingModo.Voluntariar);

        RuleFor(x => x.VendedorId).NotEmpty().WithMessage("Vendedor é obrigatório (claim sub).");
        RuleFor(x => x.CategoriaId).NotEmpty().WithMessage("Categoria é obrigatória.");

        RuleFor(x => x.Visibilidade)
            .NotEqual(ListingVisibilidade.Comunidade).WithMessage("Visibilidade Comunidade exige ComunidadeId.")
            .When(x => x.ComunidadeId is null);
    }
}
