using MediatR;
using Revoa.Abstractions;
using Revoa.IntegrationContracts.Events;
using Revoa.Moderation.Domain.Aggregates.ReportAggregate;
using Revoa.Moderation.Domain.Repositories;

namespace Revoa.Moderation.Application.Commands;

// Resolve denúncia (UF-25): admin arquiva/avisa/bane. Banned + TargetType==User publica
// UserBanRequestedEvent → Identity bane o usuário (ban via evento, sem referenciar o módulo Identity).
public sealed record ResolveReportCommand(
    string ResolvedBy,
    Guid ReportId,
    ResolutionAction Action,
    string? Note) : IRequest<Result>;

public class ResolveReportCommandHandler : IRequestHandler<ResolveReportCommand, Result>
{
    private readonly IReportRepository _reports;
    private readonly IIntegrationEventBus _bus;

    public ResolveReportCommandHandler(
        IReportRepository reports,
        IIntegrationEventBus bus)
    {
        _reports = reports;
        _bus = bus;
    }

    public async Task<Result> Handle(ResolveReportCommand request, CancellationToken ct)
    {
        try
        {
            var report = await _reports.GetByIdAsync(request.ReportId, ct);
            if (report is null)
            {
                return Result.Fail("Denúncia não encontrada.");
            }

            if (report.Status != ReportStatus.Open)
            {
                return Result.Fail("Esta denúncia já foi resolvida.");
            }

            report.Resolve(request.ResolvedBy, request.Action, request.Note);
            await _reports.UpdateAsync(report, ct);

            // Ban via evento: só dispara se a ação for Banned e o alvo for um usuário.
            // TargetId corresponde ao UserId banido. Isolamento: Moderation não referencia Identity.
            if (request.Action == ResolutionAction.Banned && report.TargetType == ReportTarget.User)
            {
                await _bus.PublishAsync(
                    new UserBanRequestedEvent(report.TargetId, "Banido por moderação: " + report.Reason),
                    ct);
            }

            return Result.Ok();
        }
        catch (DomainException ex)
        {
            return Result.Fail(ex.Message);
        }
    }
}
