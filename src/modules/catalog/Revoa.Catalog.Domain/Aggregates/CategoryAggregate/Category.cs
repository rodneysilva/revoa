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
    public string Nome { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Descricao { get; private set; }
    public CategoryStatus Status { get; private set; }

    private Category() { }

    public static Category Create(string nome, string slug, string? descricao = null)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DomainException("Nome da categoria Ã© obrigatÃ³rio.");
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new DomainException("Slug da categoria Ã© obrigatÃ³rio.");
        }

        return new Category
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Slug = slug.ToLowerInvariant(),
            Descricao = descricao,
            Status = CategoryStatus.Active,
            Version = 1
        };
    }

    public void Deactivate()
    {
        Status = CategoryStatus.Inactive;
    }
}
