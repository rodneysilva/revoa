using Revoa.Abstractions;

namespace Revoa.Catalog.Domain.Aggregates.CategoryAggregate;

public enum CategoryStatus
{
    Active,
    Inactive
}

// Aggregate "Categoria" (OOUX objeto 8). Filtro do feed; soft-delete via Status inactive.
public class Category : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public CategoryStatus Status { get; private set; }

    private Category() { }

    public static Category Create(string nome, string slug, string? descricao = null)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DomainException("Nome da categoria é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new DomainException("Slug da categoria é obrigatório.");
        }

        return new Category
        {
            Id = Guid.NewGuid(),
            Name = nome,
            Slug = slug.ToLowerInvariant(),
            Description = descricao,
            Status = CategoryStatus.Active,
            Version = 1
        };
    }

    public void Deactivate()
    {
        Status = CategoryStatus.Inactive;
    }
}
