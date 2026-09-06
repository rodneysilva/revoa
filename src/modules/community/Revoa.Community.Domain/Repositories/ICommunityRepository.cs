using Revoa.Community.Domain.Aggregates.CommunityAggregate;

namespace Revoa.Community.Domain.Repositories;

// Filtro de comunidades públicas (aplicado no MongoDB).
public record PublicFilter(CommunityAxis? Axis, string? City, int Limit);

public interface ICommunityRepository
{
    Task<CommunityGroup?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<CommunityGroup>> GetPublicAsync(PublicFilter filter, CancellationToken ct);

    // Comunidade Default de uma cidade (auto-vínculo no onboarding). Null se não existir.
    Task<CommunityGroup?> GetByCidadeETipoDefaultAsync(string cidade, CancellationToken ct);

    Task AddAsync(CommunityGroup community, CancellationToken ct);

    Task UpdateAsync(CommunityGroup community, CancellationToken ct);
}
