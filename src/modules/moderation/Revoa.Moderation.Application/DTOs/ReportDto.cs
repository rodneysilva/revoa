using Revoa.Moderation.Domain.Aggregates.ReportAggregate;

namespace Revoa.Moderation.Application.DTOs;

// Leitura de uma denúncia (GET /api/reports — admin). PascalCase conforme driver Mongo.
public sealed record ReportDto(
    Guid Id,
    Guid ReporterId,
    string ReporterName,
    ReportTarget TargetType,
    Guid TargetId,
    ReportReason Reason,
    string? Details,
    ReportStatus Status,
    ResolutionAction? Action,
    string? ResolvedBy,
    string? ResolutionNote,
    DateTime? ResolvedAt,
    DateTime CreatedAt);

public static class ReportDtoMapper
{
    public static ReportDto From(Report r) => new(
        r.Id,
        r.ReporterId,
        r.ReporterName,
        r.TargetType,
        r.TargetId,
        r.Reason,
        r.Details,
        r.Status,
        r.Action,
        r.ResolvedBy,
        r.ResolutionNote,
        r.ResolvedAt,
        r.CreatedAt);
}
