using MongoDB.Driver;
using Revoa.Exchange.Domain.Aggregates.HelpRequestAggregate;
using Revoa.Exchange.Domain.Repositories;
using Revoa.Infrastructure.Persistence;

namespace Revoa.Exchange.Infrastructure.Persistence;

public class HelpRequestsRepository : MongoRepositoryBase<HelpRequest>, IHelpRequestRepository, IMongoIndexEnsurer
{
    public HelpRequestsRepository(IMongoDatabase database) : base(database, "HelpRequests")
    {
    }

    public async Task<IReadOnlyList<HelpRequest>> GetOpenByListingAsync(Guid listingId, CancellationToken ct)
    {
        return await Collection
            .Find(h => h.ListingId == listingId && h.State == HelpRequestState.Open)
            .SortBy(h => h.CreatedAt)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Cria índices (ListingId+State, AuthorId). Idempotente.
    /// </summary>
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await Collection.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<HelpRequest>(
                Builders<HelpRequest>.IndexKeys
                    .Ascending(h => h.ListingId)
                    .Ascending(h => h.State),
                new CreateIndexOptions { Name = "ix_ListingId_State" }),
            new CreateIndexModel<HelpRequest>(
                Builders<HelpRequest>.IndexKeys.Ascending(h => h.AuthorId),
                new CreateIndexOptions { Name = "ix_AuthorId" })
        }, ct);
    }
}
