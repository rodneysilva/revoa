using MongoDB.Driver;
using Revoa.Catalog.Domain.Aggregates.ListingLikeAggregate;
using Revoa.Catalog.Domain.Repositories;
using Revoa.Infrastructure.Persistence;

namespace Revoa.Catalog.Infrastructure.Persistence;

public class ListingLikesRepository : MongoRepositoryBase<ListingLike>, IListingLikeRepository, IMongoIndexEnsurer
{
    private const int UserCap = 500;

    public ListingLikesRepository(IMongoDatabase database) : base(database, "ListingLikes")
    {
    }

    public Task<bool> ExistsAsync(Guid listingId, Guid userId, CancellationToken ct)
    {
        var fb = Builders<ListingLike>.Filter;
        return Collection.Find(fb.Eq(x => x.ListingId, listingId) & fb.Eq(x => x.UserId, userId))
            .AnyAsync(ct);
    }

    public Task AddAsync(ListingLike like, CancellationToken ct)
    {
        _ = like ?? throw new ArgumentNullException(nameof(like));
        return Collection.InsertOneAsync(like, cancellationToken: ct);
    }

    public Task RemoveAsync(Guid listingId, Guid userId, CancellationToken ct)
    {
        var fb = Builders<ListingLike>.Filter;
        return Collection.DeleteOneAsync(
            fb.Eq(x => x.ListingId, listingId) & fb.Eq(x => x.UserId, userId), ct);
    }

    public async Task<IReadOnlyList<Guid>> GetAllLikedIdsAsync(Guid userId, CancellationToken ct)
    {
        var fb = Builders<ListingLike>.Filter;
        return await Collection.Find(fb.Eq(x => x.UserId, userId))
            .SortByDescending(x => x.CreatedAt)
            .Project(x => x.ListingId)
            .Limit(UserCap)
            .ToListAsync(ct);
    }

    /// <summary>Índice único por (anúncio, usuário) — toggle idempotente. Idempotente.</summary>
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await Collection.Indexes.CreateOneAsync(
            new CreateIndexModel<ListingLike>(
                Builders<ListingLike>.IndexKeys
                    .Ascending(x => x.ListingId)
                    .Ascending(x => x.UserId),
                new CreateIndexOptions { Name = "ux_Listing_User", Unique = true }),
            cancellationToken: ct);
    }
}
