using MongoDB.Driver;
using Revoa.Catalog.Domain.Aggregates.SavedListingAggregate;
using Revoa.Catalog.Domain.Repositories;
using Revoa.Infrastructure.Persistence;

namespace Revoa.Catalog.Infrastructure.Persistence;

public class SavedListingsRepository : MongoRepositoryBase<SavedListing>, ISavedListingRepository, IMongoIndexEnsurer
{
    private const int UserCap = 200;

    public SavedListingsRepository(IMongoDatabase database) : base(database, "SavedListings")
    {
    }

    public Task<bool> ExistsAsync(Guid userId, Guid listingId, CancellationToken ct)
    {
        var fb = Builders<SavedListing>.Filter;
        return Collection.Find(fb.Eq(s => s.UserId, userId) & fb.Eq(s => s.ListingId, listingId))
            .AnyAsync(ct);
    }

    public Task AddAsync(SavedListing saved, CancellationToken ct)
    {
        _ = saved ?? throw new ArgumentNullException(nameof(saved));
        return Collection.InsertOneAsync(saved, cancellationToken: ct);
    }

    public Task RemoveAsync(Guid userId, Guid listingId, CancellationToken ct)
    {
        var fb = Builders<SavedListing>.Filter;
        return Collection.DeleteOneAsync(
            fb.Eq(s => s.UserId, userId) & fb.Eq(s => s.ListingId, listingId), ct);
    }

    public async Task<IReadOnlyList<SavedListing>> GetByUserAsync(Guid userId, CancellationToken ct)
    {
        var fb = Builders<SavedListing>.Filter;
        return await Collection.Find(fb.Eq(s => s.UserId, userId))
            .SortByDescending(s => s.CreatedAt)
            .Limit(UserCap)
            .ToListAsync(ct);
    }

    /// <summary>Índice único por (usuário, anúncio) — toggle idempotente. Idempotente.</summary>
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await Collection.Indexes.CreateOneAsync(
            new CreateIndexModel<SavedListing>(
                Builders<SavedListing>.IndexKeys
                    .Ascending(s => s.UserId)
                    .Ascending(s => s.ListingId),
                new CreateIndexOptions { Name = "ux_User_Listing", Unique = true }),
            cancellationToken: ct);
    }
}
