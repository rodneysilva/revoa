using FluentValidation;
using Revoa.Catalog.Domain.Aggregates.ListingAggregate;

namespace Revoa.Catalog.Application.Validators;

public class CreateListingCommandValidator : AbstractValidator<Commands.CreateListingCommand>
{
    public CreateListingCommandValidator()
    {
        RuleFor(x => x.Kind).IsInEnum().WithMessage("Kind inválido (Product|Service).");
        RuleFor(x => x.Mode).IsInEnum().WithMessage("Modo inválido (Trade|Resell|Donate|Volunteer).");
        RuleFor(x => x.Visibility).IsInEnum().WithMessage("Visibilidade inválida (Community|Global|Both).");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Título é obrigatório.")
            .MinimumLength(3).WithMessage("Título deve ter ao menos 3 caracteres.")
            .MaximumLength(120).WithMessage("Título deve ter no máximo 120 caracteres.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Descrição é obrigatória.")
            .MaximumLength(2000).WithMessage("Descrição deve ter no máximo 2000 caracteres.");

        RuleFor(x => x.PriceRvm)
            .GreaterThanOrEqualTo(0).WithMessage("Preço RVM deve ser ≥ 0.");

        // Doar/Voluntariar = 0 RVM.
        RuleFor(x => x.PriceRvm)
            .Equal(0).WithMessage("Doar/voluntariar deve ter preço 0 RVM.")
            .When(x => x.Mode is ListingMode.Donate or ListingMode.Volunteer);

        RuleFor(x => x.SellerId).NotEmpty().WithMessage("Vendedor é obrigatório (claim sub).");
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Categoria é obrigatória.");

        RuleFor(x => x.Visibility)
            .NotEqual(ListingVisibility.Community).WithMessage("Visibilidade Comunidade exige CommunityId.")
            .When(x => x.CommunityId is null);
    }
}
