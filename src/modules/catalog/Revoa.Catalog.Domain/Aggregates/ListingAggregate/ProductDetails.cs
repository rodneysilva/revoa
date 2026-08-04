using Revoa.Abstractions;

namespace Revoa.Catalog.Domain.Aggregates.ListingAggregate;

// VO tipado para kind=Product: Condition, Stock. NftTokenId fica no aggregate (mint-to-escrow ao listar).
public enum ProductCondition
{
    Novo,
    Seminovo,
    Usado
}

public class ProductDetails : ValueObject
{
    public ProductCondition Condition { get; private set; }
    public int Stock { get; private set; }

    private ProductDetails() { }

    public static ProductDetails Create(ProductCondition condition, int stock)
    {
        if (stock < 0)
        {
            throw new DomainException("Stock não pode ser negativo.");
        }

        return new ProductDetails { Condition = condition, Stock = stock };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Condition;
        yield return Stock;
    }
}
