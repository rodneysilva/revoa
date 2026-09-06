using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using Revoa.Community.Domain.Aggregates.PostAggregate;
using Revoa.Community.Domain.Repositories;
using Revoa.Infrastructure.Persistence;

namespace Revoa.Community.Infrastructure.Persistence;

public class PostsRepository : MongoRepositoryBase<Post>, IPostRepository, IMongoIndexEnsurer
{
    public PostsRepository(IMongoDatabase database) : base(database, "Posts")
    {
    }

    public async Task<IReadOnlyList<Post>> GetByCommunityAsync(Guid communityId, Guid? parentId, CancellationToken ct)
    {
        var fb = Builders<Post>.Filter;
        var query = fb.Eq(p => p.CommunityId, communityId)
                    & fb.Eq(p => p.Status, PostStatus.Visible);

        query &= parentId is null
            ? fb.Eq(p => p.ParentId, (Guid?)null) // null OU ausente — Exists(false) não casa BsonNull
            : fb.Eq(p => p.ParentId, parentId);

        return await Collection.Find(query)
            .SortBy(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Post>> GetByComunidadeRecentAsync(Guid communityId, int limit, CancellationToken ct)
    {
        var fb = Builders<Post>.Filter;
        var query = fb.Eq(p => p.CommunityId, communityId)
                    & fb.Eq(p => p.Status, PostStatus.Visible);

        var safeLimit = limit > 0 ? limit : 50;

        return await Collection.Find(query)
            .SortByDescending(p => p.CreatedAt)
            .Limit(safeLimit)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Post>> GetByAutorAsync(Guid autorId, int limit, CancellationToken ct)
    {
        var fb = Builders<Post>.Filter;
        var query = fb.Eq(p => p.AutorId, autorId)
                    & fb.Eq(p => p.Status, PostStatus.Visible);

        var safeLimit = limit > 0 ? limit : 20;

        return await Collection.Find(query)
            .SortByDescending(p => p.CreatedAt)
            .Limit(safeLimit)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetChildrenCountsAsync(
        IReadOnlyCollection<Guid> parentIds, CancellationToken ct)
    {
        var dict = new Dictionary<Guid, int>();
        if (parentIds.Count == 0)
        {
            return dict;
        }

        var fb = Builders<Post>.Filter;
        var query = fb.In(p => p.ParentId, parentIds.Select(id => (Guid?)id))
                    & fb.Eq(p => p.Status, PostStatus.Visible);

        var grouped = await Collection.Aggregate()
            .Match(query)
            .Group(new BsonDocument
            {
                { "_id", "$ParentId" },
                { "Count", new BsonDocument("$sum", 1) }
            })
            .ToListAsync(ct);

        foreach (var doc in grouped)
        {
            dict[doc["_id"].AsGuid] = doc["Count"].AsInt32;
        }

        return dict;
    }

    // Oculta o post e descendentes cujo Path inicia com `path` (prefix regex).
    public async Task HideCascadeAsync(string path, string ocultadoPor, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var filter = Builders<Post>.Filter.Regex(
            p => p.Path,
            new BsonRegularExpression("^" + Regex.Escape(path)));

        var update = Builders<Post>.Update
            .Set(p => p.Status, PostStatus.Hidden)
            .Set(p => p.OcultadoPor, ocultadoPor);

        await Collection.UpdateManyAsync(filter, update, cancellationToken: ct);
    }

    /// <summary>
    /// Índices de posts (comunidade+data, parentId, path p/ cascade, status). Idempotente.
    /// </summary>
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await Collection.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Post>(
                Builders<Post>.IndexKeys
                    .Ascending(p => p.CommunityId)
                    .Ascending(p => p.CreatedAt),
                new CreateIndexOptions { Name = "ix_Comunidade_CreatedAt" }),
            new CreateIndexModel<Post>(
                Builders<Post>.IndexKeys.Ascending(p => p.ParentId),
                new CreateIndexOptions { Name = "ix_ParentId", Sparse = true }),
            new CreateIndexModel<Post>(
                Builders<Post>.IndexKeys.Ascending(p => p.Path),
                new CreateIndexOptions { Name = "ix_Path" }),
            new CreateIndexModel<Post>(
                Builders<Post>.IndexKeys.Ascending(p => p.Status),
                new CreateIndexOptions { Name = "ix_Status" }),
            new CreateIndexModel<Post>(
                Builders<Post>.IndexKeys
                    .Ascending(p => p.AutorId)
                    .Descending(p => p.CreatedAt),
                new CreateIndexOptions { Name = "ix_Autor_CreatedAt" })
        }, ct);
    }
}
