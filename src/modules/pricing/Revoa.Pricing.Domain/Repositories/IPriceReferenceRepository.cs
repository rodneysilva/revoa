using Revoa.Pricing.Domain.Aggregates.PriceReferenceAggregate;

namespace Revoa.Pricing.Domain.Repositories;

public interface IPriceReferenceRepository
{
    Task<PriceReference?> GetByCategoryAsync(Guid categoriaId, CancellationToken ct);

    Task<IReadOnlyList<PriceReference>> GetAllAsync(CancellationToken ct);

    // Upsert por CategoriaId: cria se não existe, substitui se existe (sem optimistic locking —
    // a unicidade de CategoriaId é o controle de concorrência deste aggregate; o refresh sempre
    // recria o documento inteiro). Bump de Version é aqui (repositório), nunca no aggregate.
    Task UpsertAsync(PriceReference priceReference, CancellationToken ct);

    Task EnsureIndexesAsync(CancellationToken ct = default);
}
