using Revoa.Moderation.Domain.Aggregates.ReportAggregate;

namespace Revoa.Moderation.Domain.Repositories;

public interface IReportRepository
{
    Task<Report?> GetByIdAsync(Guid id, CancellationToken ct = default);

    // Lista paginada por status (mais recentes primeiro). status null = todas.
    Task<IReadOnlyList<Report>> GetByStatusAsync(ReportStatus? status, int page, int pageSize, CancellationToken ct = default);

    // Denúncias feitas por um usuário.
    Task<IReadOnlyList<Report>> GetByReporterAsync(Guid reporterId, CancellationToken ct = default);

    Task AddAsync(Report report, CancellationToken ct = default);

    Task UpdateAsync(Report report, CancellationToken ct = default);

    Task EnsureIndexesAsync(CancellationToken ct = default);
}
