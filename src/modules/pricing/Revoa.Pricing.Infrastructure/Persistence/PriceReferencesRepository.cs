using MongoDB.Driver;
using Revoa.Pricing.Domain.Aggregates.PriceReferenceAggregate;
using Revoa.Pricing.Domain.Repositories;
using Revoa.Infrastructure.Persistence;

namespace Revoa.Pricing.Infrastructure.Persistence;

public class PriceReferencesRepository : IPriceReferenceRepository, IMongoIndexEnsurer
{
    private readonly IMongoCollection<PriceReference> _priceReferences;

    public PriceReferencesRepository(IMongoDatabase database)
    {
        _priceReferences = database.GetCollection<PriceReference>("PriceReferences");
    }

    public async Task<PriceReference?> GetByCategoryAsync(Guid categoriaId, CancellationToken ct)
    {
        return await _priceReferences.Find(p => p.CategoriaId == categoriaId).FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<PriceReference>> GetAllAsync(CancellationToken ct)
    {
        return await _priceReferences
            .Find(_ => true)
            .SortByDescending(p => p.UpdatedAt)
            .ToListAsync(ct);
    }

    // Upsert por CategoriaId (IsUpsert=true): cria se não existe, substitui se existe.
    // Bump de Version é aqui (repositório), nunca no aggregate. Sem optimistic locking —
    // a unicidade de CategoriaId é o controle deste aggregate (refresh sempre recria o doc).
    public async Task UpsertAsync(PriceReference priceReference, CancellationToken ct)
    {
        priceReference.IncrementVersion();

        var filter = Builders<PriceReference>.Filter.Eq(p => p.CategoriaId, priceReference.CategoriaId);
        await _priceReferences.ReplaceOneAsync(
            filter, priceReference, new ReplaceOptions { IsUpsert = true }, ct);
    }

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await _priceReferences.Indexes.CreateOneAsync(
            new CreateIndexModel<PriceReference>(
                Builders<PriceReference>.IndexKeys.Ascending(p => p.CategoriaId),
                new CreateIndexOptions { Name = "ux_CategoriaId", Unique = true }),
            cancellationToken: ct);
    }
}
