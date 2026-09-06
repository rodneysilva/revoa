using MongoDB.Driver;
using Revoa.Abstractions;
using Revoa.Catalog.Domain.Aggregates.CategoryAggregate;
using Revoa.Catalog.Domain.Repositories;
using Revoa.Infrastructure.Persistence;

namespace Revoa.Catalog.Infrastructure.Persistence;

public class CategoriesRepository : MongoRepositoryBase<Category>, ICategoryRepository, IMongoIndexEnsurer
{
    public CategoriesRepository(IMongoDatabase database) : base(database, "Categories")
    {
    }

    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken ct)
    {
        return await Collection.Find(c => c.Slug == slug).FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<Category>> ListActiveAsync(CancellationToken ct)
    {
        return await Collection.Find(c => c.Status == CategoryStatus.Active)
            .SortBy(c => c.Nome)
            .ToListAsync(ct);
    }

    // Traduz violação de índice único (Slug) em exceção de domínio.
    public override async Task AddAsync(Category category, CancellationToken ct = default)
    {
        try
        {
            await Collection.InsertOneAsync(category, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new DuplicateKeyException("Já existe uma categoria com esse slug.");
        }
    }

    /// <summary>
    /// Índice único em Slug. Idempotente.
    /// </summary>
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        var keys = Builders<Category>.IndexKeys.Ascending(c => c.Slug);
        await Collection.Indexes.CreateOneAsync(
            new CreateIndexModel<Category>(keys, new CreateIndexOptions { Name = "ux_Slug", Unique = true }),
            cancellationToken: ct);
    }

    /// <summary>
    /// Popula as categorias canônicas (CategorySeed) se a coleção estiver vazia (dev/onboarding).
    /// Idempotente.
    /// </summary>
    public async Task EnsureSeedAsync(CancellationToken ct = default)
    {
        if (await Collection.EstimatedDocumentCountAsync(cancellationToken: ct) > 0)
        {
            return;
        }

        foreach (var (nome, slug, descricao) in CategorySeed.All)
        {
            try
            {
                await Collection.InsertOneAsync(Category.Create(nome, slug, descricao), cancellationToken: ct);
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                // Slug já existe (race/legado) — ignora.
            }
        }
    }
}
