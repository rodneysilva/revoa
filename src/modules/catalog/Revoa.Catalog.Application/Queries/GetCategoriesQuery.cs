using MediatR;
using Revoa.Abstractions;
using Revoa.Catalog.Application.DTOs;
using Revoa.Catalog.Domain.Repositories;

namespace Revoa.Catalog.Application.Queries;

// Lista categorias ativas (dropdown do Criar Anúncio + filtros do feed).
public sealed record GetCategoriesQuery : IRequest<Result<IReadOnlyList<CategoryDto>>>;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, Result<IReadOnlyList<CategoryDto>>>
{
    private readonly ICategoryRepository _categories;

    public GetCategoriesQueryHandler(ICategoryRepository categories)
    {
        _categories = categories;
    }

    public async Task<Result<IReadOnlyList<CategoryDto>>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        var items = await _categories.ListActiveAsync(ct);
        return Result<IReadOnlyList<CategoryDto>>.Ok(items.Select(CategoryDtoMapper.From).ToList());
    }
}
