using MongoDB.Driver;
using Revoa.Abstractions;
using Revoa.Catalog.Domain.Aggregates.CategoryAggregate;
using Revoa.Catalog.Domain.Repositories;

namespace Revoa.Catalog.Infrastructure.Persistence;

public class CategoriesRepository : ICategoryRepository
{
    private readonly IMongoCollection<Category> _categories;

    public CategoriesRepository(IMongoDatabase database)
    {
        _categories = database.GetCollection<Category>("Categories");
    }

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _categories.Find(c => c.Id == id).FirstOrDefaultAsync(ct);
    }

    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken ct)
    {
        return await _categories.Find(c => c.Slug == slug).FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<Category>> ListActiveAsync(CancellationToken ct)
    {
        return await _categories.Find(c => c.Status == CategoryStatus.Active)
            .SortBy(c => c.Nome)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Category category, CancellationToken ct)
    {
        try
        {
            await _categories.InsertOneAsync(category, cancellationToken: ct);
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
        await _categories.Indexes.CreateOneAsync(
            new CreateIndexModel<Category>(keys, new CreateIndexOptions { Name = "ux_Slug", Unique = true }),
            cancellationToken: ct);
    }
}
