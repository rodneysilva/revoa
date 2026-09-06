using Revoa.Catalog.Domain.Aggregates.CategoryAggregate;

namespace Revoa.Catalog.Application.DTOs;

public sealed record CategoryDto(Guid Id, string Name, string Slug, string? Description);

public static class CategoryDtoMapper
{
    public static CategoryDto From(Category c) =>
        new(c.Id, c.Name, c.Slug, c.Description);
}
