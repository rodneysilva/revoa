using MongoDB.Bson;
using MongoDB.Driver;
using Revoa.Catalog.Domain.Aggregates.CommentAggregate;
using Revoa.Catalog.Domain.Repositories;
using Revoa.Infrastructure.Persistence;

namespace Revoa.Catalog.Infrastructure.Persistence;

public class CommentsRepository : MongoRepositoryBase<Comment>, ICommentRepository, IMongoIndexEnsurer
{
    public CommentsRepository(IMongoDatabase database) : base(database, "Comments")
    {
    }

    public async Task<IReadOnlyList<Comment>> GetByListingAsync(Guid listingId, Guid? parentId, CancellationToken ct)
    {
        var fb = Builders<Comment>.Filter;
        var query = fb.Eq(c => c.ListingId, listingId)
                    & fb.Eq(c => c.Status, CommentStatus.Visible);

        query &= parentId is null
            ? fb.Eq(c => c.ParentId, (Guid?)null) // null OU ausente — Exists(false) não casa BsonNull
            : fb.Eq(c => c.ParentId, parentId);

        return await Collection.Find(query)
            .SortBy(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    // Mesmo formato de PostsRepository.GetChildrenCountsAsync (aggregation $group).
    public async Task<IReadOnlyDictionary<Guid, int>> GetCountsAsync(
        IReadOnlyCollection<Guid> listingIds, CancellationToken ct)
    {
        var dict = new Dictionary<Guid, int>();
        if (listingIds.Count == 0)
        {
            return dict;
        }

        var fb = Builders<Comment>.Filter;
        var query = fb.In(c => c.ListingId, listingIds)
                    & fb.Eq(c => c.Status, CommentStatus.Visible);

        var grouped = await Collection.Aggregate()
            .Match(query)
            .Group(new BsonDocument
            {
                { "_id", "$ListingId" },
                { "Count", new BsonDocument("$sum", 1) }
            })
            .ToListAsync(ct);

        foreach (var doc in grouped)
        {
            dict[doc["_id"].AsBsonBinaryData.ToGuid()] = doc["Count"].AsInt32;
        }

        return dict;
    }

    // Espelho do PostsRepository.GetChildrenCountsAsync (aggregation $group por ParentId).
    public async Task<IReadOnlyDictionary<Guid, int>> GetChildrenCountsAsync(
        IReadOnlyCollection<Guid> parentIds, CancellationToken ct)
    {
        var dict = new Dictionary<Guid, int>();
        if (parentIds.Count == 0)
        {
            return dict;
        }

        var fb = Builders<Comment>.Filter;
        var query = fb.In(c => c.ParentId, parentIds.Select(id => (Guid?)id))
                    & fb.Eq(c => c.Status, CommentStatus.Visible);

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

    /// <summary>Índices (listing+data, parentId, path). Idempotente.</summary>
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await Collection.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Comment>(
                Builders<Comment>.IndexKeys.Ascending(c => c.ListingId).Ascending(c => c.CreatedAt),
                new CreateIndexOptions { Name = "ix_Listing_CreatedAt" }),
            new CreateIndexModel<Comment>(
                Builders<Comment>.IndexKeys.Ascending(c => c.ParentId),
                new CreateIndexOptions { Name = "ix_ParentId", Sparse = true }),
            new CreateIndexModel<Comment>(
                Builders<Comment>.IndexKeys.Ascending(c => c.Path),
                new CreateIndexOptions { Name = "ix_Path" }),
        }, ct);
    }
}
