using Revoa.Community.Domain.Aggregates.MembershipAggregate;

namespace Revoa.Community.Domain.Repositories;

public interface IMembershipRepository
{
    Task<Membership?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<Membership?> GetByUsuarioEComunidadeAsync(Guid usuarioId, Guid comunidadeId, CancellationToken ct);

    Task<IReadOnlyList<Membership>> ListByComunidadeAsync(Guid comunidadeId, CancellationToken ct);

    Task<IReadOnlyList<Membership>> ListByUsuarioAsync(Guid usuarioId, CancellationToken ct);

    // Contagem batch por comunidade (anti-N+1 no feed de comunidades). Só memberships Ativa.
    Task<IReadOnlyDictionary<Guid, int>> CountAtivasByComunidadeAsync(IEnumerable<Guid> comunidadeIds, CancellationToken ct);

    Task AddAsync(Membership membership, CancellationToken ct);

    Task UpdateAsync(Membership membership, CancellationToken ct);

    Task DeleteAsync(Membership membership, CancellationToken ct);
}
