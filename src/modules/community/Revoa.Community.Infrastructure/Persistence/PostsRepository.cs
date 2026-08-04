using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using Revoa.Abstractions;
using Revoa.Community.Domain.Aggregates.PostAggregate;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Infrastructure.Persistence;

public class PostsRepository : IPostRepository
{
    private readonly IMongoCollection<Post> _posts;

    public PostsRepository(IMongoDatabase database)
    {
        _posts = database.GetCollection<Post>("Posts");
    }

    public async Task<Post?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _posts.Find(p => p.Id == id).FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<Post>> GetByComunidadeAsync(Guid comunidadeId, Guid? parentId, CancellationToken ct)
    {
        var fb = Builders<Post>.Filter;
        var query = fb.Eq(p => p.ComunidadeId, comunidadeId)
                    & fb.Eq(p => p.Status, PostStatus.Visivel);

        query &= parentId is null
            ? fb.Eq(p => p.ParentId, (Guid?)null) // null OU ausente — Exists(false) não casa BsonNull
            : fb.Eq(p => p.ParentId, parentId);

        return await _posts.Find(query)
            .SortBy(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Post>> GetByComunidadeRecentAsync(Guid comunidadeId, int limit, CancellationToken ct)
    {
        var fb = Builders<Post>.Filter;
        var query = fb.Eq(p => p.ComunidadeId, comunidadeId)
                    & fb.Eq(p => p.Status, PostStatus.Visivel);

        var safeLimit = limit > 0 ? limit : 50;

        return await _posts.Find(query)
            .SortByDescending(p => p.CreatedAt)
            .Limit(safeLimit)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Post post, CancellationToken ct)
    {
        await _posts.InsertOneAsync(post, cancellationToken: ct);
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
            .Set(p => p.Status, PostStatus.Oculto)
            .Set(p => p.OcultadoPor, ocultadoPor);

        await _posts.UpdateManyAsync(filter, update, cancellationToken: ct);
    }

    public async Task UpdateAsync(Post post, CancellationToken ct)
    {
        var expectedVersion = post.Version;

        var filter = Builders<Post>.Filter.Eq(p => p.Id, post.Id)
                     & (Builders<Post>.Filter.Eq(p => p.Version, expectedVersion)
                        | Builders<Post>.Filter.Exists(p => p.Version, false));

        post.IncrementVersion();

        var result = await _posts.ReplaceOneAsync(filter, post, cancellationToken: ct);
        if (result.MatchedCount == 0)
        {
            throw new ConcurrencyException(post.Id.ToString(), expectedVersion);
        }
    }

    /// <summary>
    /// Índices de posts (comunidade+data, parentId, path p/ cascade, status). Idempotente.
    /// </summary>
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await _posts.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Post>(
                Builders<Post>.IndexKeys
                    .Ascending(p => p.ComunidadeId)
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
                new CreateIndexOptions { Name = "ix_Status" })
        }, ct);
    }
}
