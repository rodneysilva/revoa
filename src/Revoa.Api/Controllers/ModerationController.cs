using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Revoa.Moderation.Application.Commands;
using Revoa.Moderation.Application.DTOs;
using Revoa.Moderation.Application.Queries;
using Revoa.Moderation.Domain.Aggregates.ReportAggregate;

namespace Revoa.Api.Controllers;

// Moderação (UF-24/25): denúncias + resolução admin. Denunciar exige login+verificação (Verified);
// listar e resolver denúncias é restrito a admins (Policy Admin). O banimento de usuário acontece
// via evento (UserBanRequestedEvent → Identity), mantendo os módulos isolados.
[ApiController]
[Route("api/reports")]
public class ModerationController : ControllerBase
{
    private readonly IMediator _mediator;

    public ModerationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Cria denúncia (anúncio/post/usuário/comentário). Gate Verified. Reporter vem do token.
    [HttpPost]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult> Create([FromBody] CreateReportRequest request, CancellationToken ct)
    {
        var (reporterId, nome, unauthorized) = ReadUser();
        if (unauthorized is not null)
        {
            return Unauthorized(unauthorized);
        }

        if (!Enum.TryParse<ReportTarget>(request.TargetType, ignoreCase: true, out var targetType)
            || !Enum.TryParse<ReportReason>(request.Reason, ignoreCase: true, out var reason))
        {
            return BadRequest(new { error = "Tipo de alvo ou motivo de denúncia inválido." });
        }

        if (!Guid.TryParse(request.TargetId, out var targetId) || targetId == Guid.Empty)
        {
            return BadRequest(new { error = "Alvo da denúncia inválido." });
        }

        var result = await _mediator.Send(
            new CreateReportCommand(reporterId, nome, targetType, targetId, reason, request.Details), ct);

        return result.IsFailure
            ? BadRequest(new { error = result.Error })
            : Ok(new { id = result.Value });
    }

    // Lista denúncias para o painel admin (filtrável por status). Gate Admin.
    [HttpGet]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<IReadOnlyList<ReportDto>>> List(
        [FromQuery] string? status, [FromQuery] int page = 1, CancellationToken ct = default)
    {
        ReportStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<ReportStatus>(status, ignoreCase: true, out var parsed))
        {
            statusFilter = parsed;
        }

        var result = await _mediator.Send(new GetReportsQuery(statusFilter, page), ct);
        return result.IsFailure ? BadRequest(new { error = result.Error }) : Ok(result.Value);
    }

    // Resolve denúncia (arquivar/avisar/banir). Gate Admin. resolvedBy = e-mail do admin (claim email).
    [HttpPost("{id:guid}/resolve")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult> Resolve(
        Guid id, [FromBody] ResolveReportRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<ResolutionAction>(request.Action, ignoreCase: true, out var action))
        {
            return BadRequest(new { error = "Ação de resolução inválida." });
        }

        var resolvedBy = User.FindFirst("email")?.Value
                         ?? User.FindFirst("name")?.Value
                         ?? "Admin";

        var result = await _mediator.Send(
            new ResolveReportCommand(resolvedBy, id, action, request.Note), ct);

        return result.IsFailure ? BadRequest(new { error = result.Error }) : Ok();
    }

    // ReporterId/Nome vêm do token (claim sub/name), nunca do body.
    private (Guid reporterId, string nome, object? unauthorized) ReadUser()
    {
        var sub = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var reporterId))
        {
            return (Guid.Empty, "Usuário", new { error = "Token sem claim 'sub'." });
        }

        var nome = User.FindFirst("name")?.Value
                   ?? User.FindFirst("nickname")?.Value
                   ?? "Usuário";

        return (reporterId, nome, null);
    }
}

public sealed record CreateReportRequest(string TargetType, string TargetId, string Reason, string? Details);
public sealed record ResolveReportRequest(string Action, string? Note);
