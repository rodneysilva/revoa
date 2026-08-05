using MongoDB.Driver;
using Revoa.Abstractions;
using Revoa.Moderation.Domain.Aggregates.ReportAggregate;
using Revoa.Moderation.Domain.Repositories;

namespace Revoa.Moderation.Infrastructure.Persistence;

public class ReportsRepository : IReportRepository
{
    private readonly IMongoCollection<Report> _reports;

    public ReportsRepository(IMongoDatabase database)
    {
        _reports = database.GetCollection<Report>("Reports");
    }

    public async Task<Report?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _reports.Find(r => r.Id == id).FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<Report>> GetByStatusAsync(
        ReportStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var safePage = page > 0 ? page : 1;
        var safeSize = pageSize > 0 ? pageSize : 50;

        var query = status is null
            ? Builders<Report>.Filter.Empty
            : Builders<Report>.Filter.Eq(r => r.Status, status.Value);

        return await _reports.Find(query)
            .SortByDescending(r => r.CreatedAt)
            .Skip((safePage - 1) * safeSize)
            .Limit(safeSize)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Report>> GetByReporterAsync(Guid reporterId, CancellationToken ct = default)
    {
        return await _reports.Find(r => r.ReporterId == reporterId)
            .SortByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Report report, CancellationToken ct = default)
    {
        await _reports.InsertOneAsync(report, cancellationToken: ct);
    }

    // Optimistic locking: _id + (Version == esperada OU doc legado sem Version). Bump de Version
    // é AQUI (repositório), nunca no mutator do aggregate. MatchedCount==0 → ConcurrencyException.
    public async Task UpdateAsync(Report report, CancellationToken ct = default)
    {
        var expectedVersion = report.Version;

        var filter = Builders<Report>.Filter.Eq(r => r.Id, report.Id)
                     & (Builders<Report>.Filter.Eq(r => r.Version, expectedVersion)
                        | Builders<Report>.Filter.Exists(r => r.Version, false));

        report.IncrementVersion();

        var result = await _reports.ReplaceOneAsync(filter, report, cancellationToken: ct);
        if (result.MatchedCount == 0)
        {
            throw new ConcurrencyException(report.Id.ToString(), expectedVersion);
        }
    }

    // ix_Status_CreatedAt (painel admin: denúncias em aberto, mais recentes primeiro),
    // ix_ReporterId (denúncias por usuário), ix_Target (todas denúncias de um alvo). Idempotente.
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await _reports.Indexes.CreateManyAsync(new[]
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
