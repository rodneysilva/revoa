using MongoDB.Driver;
using Revoa.Abstractions;

namespace Revoa.Infrastructure.Persistence;

/// <summary>
/// Base de repositório Mongo: coleção própria + operações padrão com locking otimista
/// (bloco antes copiado à mão em 11 repositórios). O repositório derivado mantém apenas
/// queries customizadas + EnsureIndexesAsync; sobrescreve AddAsync/UpdateAsync quando o
/// comportamento difere (ex.: tradução de DuplicateKeyException em erro de domínio).
/// </summary>
public abstract class MongoRepositoryBase<T> where T : Entity
{
    protected readonly IMongoCollection<T> Collection;

    protected MongoRepositoryBase(IMongoDatabase database, string collectionName)
    {
        Collection = database.GetCollection<T>(collectionName);
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await Collection.Find(e => e.Id == id).FirstOrDefaultAsync(ct);
    }

    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
    {
        await Collection.InsertOneAsync(entity, cancellationToken: ct);
    }

    // Optimistic locking: _id + (Version == esperada OU doc legado sem Version).
    public virtual async Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        var expectedVersion = entity.Version;

        var filter = Builders<T>.Filter.Eq(e => e.Id, entity.Id)
                     & (Builders<T>.Filter.Eq(e => e.Version, expectedVersion)
                        | Builders<T>.Filter.Exists(e => e.Version, false));

        entity.IncrementVersion();

        var result = await Collection.ReplaceOneAsync(filter, entity, cancellationToken: ct);
        if (result.MatchedCount == 0)
        {
            throw new ConcurrencyException(entity.Id.ToString(), expectedVersion);
        }
    }
}
