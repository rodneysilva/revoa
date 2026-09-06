using Revoa.Community.Domain.Aggregates.MembershipAggregate;

namespace Revoa.Community.Domain.Repositories;

public interface IMembershipRepository
{
    Task<Membership?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<Membership?> GetByUserAndCommunityAsync(Guid userId, Guid communityId, CancellationToken ct);

    Task<IReadOnlyList<Membership>> ListByCommunityAsync(Guid communityId, CancellationToken ct);

    Task<IReadOnlyList<Membership>> ListByUsuarioAsync(Guid userId, CancellationToken ct);

    // Contagem batch por comunidade (anti-N+1 no feed de comunidades). Só memberships Active.
    Task<IReadOnlyDictionary<Guid, int>> CountActiveByCommunityAsync(IEnumerable<Guid> comunidadeIds, CancellationToken ct);

    Task AddAsync(Membership membership, CancellationToken ct);

    Task UpdateAsync(Membership membership, CancellationToken ct);

    Task DeleteAsync(Membership membership, CancellationToken ct);
}
