using MediatR;
using Revoa.Abstractions;
using Revoa.Moderation.Application.DTOs;
using Revoa.Moderation.Domain.Aggregates.ReportAggregate;
using Revoa.Moderation.Domain.Repositories;

namespace Revoa.Moderation.Application.Queries;

// Lista denúncias para o painel admin (UF-25). Filtra por status (Open/Resolved) ou traz todas,
// paginadas (mais recentes primeiro). Gate Admin no controller.
public sealed record GetReportsQuery(ReportStatus? Status, int Page) : IRequest<Result<IReadOnlyList<ReportDto>>>;

public class GetReportsQueryHandler : IRequestHandler<GetReportsQuery, Result<IReadOnlyList<ReportDto>>>
{
    private const int PageSize = 50;
    private readonly IReportRepository _reports;

    public GetReportsQueryHandler(IReportRepository reports)
    {
        _reports = reports;
    }

    public async Task<Result<IReadOnlyList<ReportDto>>> Handle(GetReportsQuery request, CancellationToken ct)
    {
        var page = request.Page > 0 ? request.Page : 1;
        var reports = await _reports.GetByStatusAsync(request.Status, page, PageSize, ct);
        var dtos = reports.Select(ReportDtoMapper.From).ToList();
        return Result<IReadOnlyList<ReportDto>>.Ok(dtos);
    }
}
