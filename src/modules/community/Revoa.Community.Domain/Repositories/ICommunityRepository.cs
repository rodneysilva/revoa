using Revoa.Community.Domain.Aggregates.CommunityAggregate;

namespace Revoa.Community.Domain.Repositories;

// Filtro de comunidades públicas (aplicado no MongoDB).
public record PublicFilter(CommunityAxis? Axis, string? City, int Limit);

public interface ICommunityRepository
{
    Task<CommunityGroup?> GetByIdAsync(Guid id, CancellationToken ct);

    // Comunidades por ids (batch $in — alimenta o feed público de posts).
    Task<IReadOnlyList<CommunityGroup>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken ct);

    Task<IReadOnlyList<CommunityGroup>> GetPublicAsync(PublicFilter filter, CancellationToken ct);

    // Painel admin: TODAS (inclui arquivadas), mais recentes primeiro, cap 500.
    Task<IReadOnlyList<CommunityGroup>> GetAllAsync(int limit, CancellationToken ct);

    // Comunidade Default de uma cidade (auto-vínculo no onboarding). Null se não existir.
    Task<CommunityGroup?> GetByCidadeETipoDefaultAsync(string cidade, CancellationToken ct);

    Task AddAsync(CommunityGroup community, CancellationToken ct);

    Task UpdateAsync(CommunityGroup community, CancellationToken ct);
}
