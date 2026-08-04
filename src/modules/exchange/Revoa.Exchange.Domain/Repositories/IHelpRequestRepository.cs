using Revoa.Exchange.Domain.Aggregates.HelpRequestAggregate;

namespace Revoa.Exchange.Domain.Repositories;

public interface IHelpRequestRepository
{
    Task<HelpRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<HelpRequest>> GetOpenByListingAsync(Guid listingId, CancellationToken ct = default);

    Task AddAsync(HelpRequest helpRequest, CancellationToken ct = default);

    Task UpdateAsync(HelpRequest helpRequest, CancellationToken ct = default);

    Task EnsureIndexesAsync(CancellationToken ct = default);
}
