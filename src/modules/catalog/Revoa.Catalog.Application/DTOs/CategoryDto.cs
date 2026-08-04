using Revoa.Catalog.Domain.Aggregates.CategoryAggregate;

namespace Revoa.Catalog.Application.DTOs;

public sealed record CategoryDto(Guid Id, string Nome, string Slug, string? Descricao);

public static class CategoryDtoMapper
{
    public static CategoryDto From(Category c) =>
        new(c.Id, c.Nome, c.Slug, c.Descricao);
}
