using MediatR;
using Revoa.Abstractions;
using Revoa.Moderation.Domain.Aggregates.ReportAggregate;
using Revoa.Moderation.Domain.Repositories;

namespace Revoa.Moderation.Application.Commands;

// Cria denúncia (UF-24): usuário verificado denuncia um alvo (anúncio/post/usuário/comentário).
// ReporterId/Nome vêm do token, nunca do body. Gate Verified no controller.
public sealed record CreateReportCommand(
    Guid ReporterId,
    string ReporterNome,
    ReportTarget TargetType,
    Guid TargetId,
    ReportReason Reason,
    string? Details) : IRequest<Result<string>>;

public class CreateReportCommandHandler : IRequestHandler<CreateReportCommand, Result<string>>
{
    private readonly IReportRepository _reports;

    public CreateReportCommandHandler(IReportRepository reports)
    {
        _reports = reports;
    }

    public async Task<Result<string>> Handle(CreateReportCommand request, CancellationToken ct)
    {
        try
        {
            var report = Report.Create(
                request.ReporterId,
                request.ReporterNome,
                request.TargetType,
                request.TargetId,
                request.Reason,
                request.Details);

            await _reports.AddAsync(report, ct);

            return Result<string>.Ok(report.Id.ToString());
        }
        catch (DomainException ex)
        {
            return Result<string>.Fail(ex.Message);
        }
    }
}
