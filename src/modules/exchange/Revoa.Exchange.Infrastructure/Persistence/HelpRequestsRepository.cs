using MongoDB.Driver;
using Revoa.Abstractions;
using Revoa.Exchange.Domain.Aggregates.HelpRequestAggregate;
using Revoa.Exchange.Domain.Repositories;

namespace Revoa.Exchange.Infrastructure.Persistence;

public class HelpRequestsRepository : IHelpRequestRepository
{
    private readonly IMongoCollection<HelpRequest> _helpRequests;

    public HelpRequestsRepository(IMongoDatabase database)
    {
        _helpRequests = database.GetCollection<HelpRequest>("HelpRequests");
    }

    public async Task<HelpRequest?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _helpRequests.Find(h => h.Id == id).FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<HelpRequest>> GetOpenByListingAsync(Guid listingId, CancellationToken ct)
    {
        return await _helpRequests
            .Find(h => h.ListingId == listingId && h.State == HelpRequestState.Open)
            .SortBy(h => h.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(HelpRequest helpRequest, CancellationToken ct)
    {
        await _helpRequests.InsertOneAsync(helpRequest, cancellationToken: ct);
    }

    public async Task UpdateAsync(HelpRequest helpRequest, CancellationToken ct)
    {
        var expectedVersion = helpRequest.Version;

        // Optimistic locking: _id + (Version == esperada OU doc legado sem Version).
        var filter = Builders<HelpRequest>.Filter.Eq(h => h.Id, helpRequest.Id)
                     & (Builders<HelpRequest>.Filter.Eq(h => h.Version, expectedVersion)
                        | Builders<HelpRequest>.Filter.Exists(h => h.Version, false));

        helpRequest.IncrementVersion();

        var result = await _helpRequests.ReplaceOneAsync(filter, helpRequest, cancellationToken: ct);
        if (result.MatchedCount == 0)
        {
            throw new ConcurrencyException(helpRequest.Id.ToString(), expectedVersion);
        }
    }

    /// <summary>
    /// Cria índices (ListingId+State, AuthorId). Idempotente.
    /// </summary>
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await _helpRequests.Indexes.CreateManyAsync(new[]
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
