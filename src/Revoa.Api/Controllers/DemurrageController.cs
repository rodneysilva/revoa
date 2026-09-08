using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Timeouts;
using Revoa.Abstractions;
using Revoa.Demurrage.Application.Commands;
using Revoa.Demurrage.Application.DTOs;
using Revoa.Demurrage.Application.Queries;

namespace Revoa.Api.Controllers;

// Demurrage (UF-27, Fase 3): queima periódica de uma % do RVM ocioso (piso de isenção + taxa
// ajustável). Tudo gated Admin. Preview calcula sem queimar; Run executa as queimas on-chain
// (faucet BURNER_ROLE) e registra em DemurrageRuns. Runs = histórico.
//
// Timeouts generosos: preview (muitas balanceOf) e run (1 tx/carteira). As queimas on-chain usam
// CT.None internamente, então um timeout/disconexão do request não aborta queimas já disparadas.
[ApiController]
[Route("api/demurrage")]
public class DemurrageController : ControllerBase
{
    private readonly IMediator _mediator;

    public DemurrageController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Pré-visualização (sem queimar). Pode demorar: 1 balanceOf por carteira.
    [HttpPost("preview")]
    [Authorize(Policy = "Admin")]
    [RequestTimeout(120000)]
    public async Task<ActionResult<DemurragePreviewDto>> Preview(CancellationToken ct)
    {
        var result = await _mediator.Send(new PreviewDemurrageCommand(), ct);
        return result.IsFailure
            ? BadRequest(new ApiError(result.Error))
            : Ok(result.Value);
    }

    // Executa as queimas on-chain. Body opcional { "executedBy": "..." }; se omitido, usa o e-mail/sub
    // do admin autenticado. Lento: 1 tx/carteira (timeout 300s). Queimas em andamento não são
    // abortadas por timeout/disconexão (CT.None interno).
    [HttpPost("run")]
    [Authorize(Policy = "Admin")]
    [RequestTimeout(300000)]
    public async Task<ActionResult<DemurrageRunDto>> Run([FromBody] RunDemurrageRequest? body, CancellationToken ct)
    {
        var executedBy = !string.IsNullOrWhiteSpace(body?.ExecutedBy)
            ? body.ExecutedBy
            : User.FindFirst("email")?.Value ?? User.FindFirst("sub")?.Value ?? "admin";

        var result = await _mediator.Send(new RunDemurrageCommand(executedBy), ct);
        return result.IsFailure
            ? BadRequest(new ApiError(result.Error))
            : Ok(result.Value);
    }

    // Histórico de execuções (RunAt desc). ?limit=20 (default).
    [HttpGet("runs")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<IReadOnlyList<DemurrageRunDto>>> Runs(
        [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetDemurrageRunsQuery(limit), ct);
        return result.IsFailure
            ? BadRequest(new ApiError(result.Error))
            : Ok(result.Value);
    }

    // Situação do reajuste IPCA: acumulado do trimestre (BCB série 433), taxa atual (runtime)
    // e a taxa que o scheduler aplicaria no próximo fechamento trimestral (jan/abr/jul/out).
    [HttpGet("ipca")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<IpcaStatusDto>> Ipca(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetIpcaQuery(), ct);
        return result.IsFailure
            ? BadRequest(new ApiError(result.Error))
            : Ok(result.Value);
    }
}

// Body do POST /run. executedBy é opcional (default = admin autenticado).
public sealed record RunDemurrageRequest(string? ExecutedBy);
