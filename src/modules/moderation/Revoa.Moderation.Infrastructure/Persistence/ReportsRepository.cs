using MongoDB.Driver;
using Revoa.Moderation.Domain.Aggregates.ReportAggregate;
using Revoa.Moderation.Domain.Repositories;
using Revoa.Infrastructure.Persistence;

namespace Revoa.Moderation.Infrastructure.Persistence;

public class ReportsRepository : MongoRepositoryBase<Report>, IReportRepository, IMongoIndexEnsurer
{
    public ReportsRepository(IMongoDatabase database) : base(database, "Reports")
    {
    }

    public async Task<IReadOnlyList<Report>> GetByStatusAsync(
        ReportStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var safePage = page > 0 ? page : 1;
        var safeSize = pageSize > 0 ? pageSize : 50;

        var query = status is null
            ? Builders<Report>.Filter.Empty
            : Builders<Report>.Filter.Eq(r => r.Status, status.Value);

        return await Collection.Find(query)
            .SortByDescending(r => r.CreatedAt)
            .Skip((safePage - 1) * safeSize)
            .Limit(safeSize)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Report>> GetByReporterAsync(Guid reporterId, CancellationToken ct = default)
    {
        return await Collection.Find(r => r.ReporterId == reporterId)
            .SortByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
    }

    // ix_Status_CreatedAt (painel admin: denúncias em aberto, mais recentes primeiro),
    // ix_ReporterId (denúncias por usuário), ix_Target (todas denúncias de um alvo). Idempotente.
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await Collection.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Report>(
                Builders<Report>.IndexKeys
                    .Ascending(r => r.Status)
                    .Descending(r => r.CreatedAt),
                new CreateIndexOptions { Name = "ix_Status_CreatedAt" }),
            new CreateIndexModel<Report>(
                Builders<Report>.IndexKeys.Ascending(r => r.ReporterId),
                new CreateIndexOptions { Name = "ix_ReporterId" }),
            new CreateIndexModel<Report>(
                Builders<Report>.IndexKeys
                    .Ascending(r => r.TargetType)
                    .Ascending(r => r.TargetId),
                new CreateIndexOptions { Name = "ix_Target" })
        }, ct);
    }
}
