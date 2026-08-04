using Revoa.Catalog.Domain.Aggregates.CategoryAggregate;

namespace Revoa.Catalog.Domain.Repositories;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<Category?> GetBySlugAsync(string slug, CancellationToken ct);

    Task<IReadOnlyList<Category>> ListActiveAsync(CancellationToken ct);

    Task AddAsync(Category category, CancellationToken ct);
}
