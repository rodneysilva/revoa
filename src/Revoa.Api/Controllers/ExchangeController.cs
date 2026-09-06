using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Revoa.Abstractions;
using Revoa.Exchange.Application.Commands;
using Revoa.Exchange.Application.DTOs;
using Revoa.Exchange.Application.Queries;

namespace Revoa.Api.Controllers;

[ApiController]
[Route("api/trades")]
public class ExchangeController : ControllerBase
{
    private readonly IMediator _mediator;

    public ExchangeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Detalhe de uma troca (anônimo vê — UF-11/12).
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<TradeSummaryDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTradeQuery(id), ct);
        return result.IsFailure ? NotFound(new { error = result.Error }) : Ok(result.Value);
    }

    // Histórico de trocas (anônimo vê). Filtro opcional por buyer/seller, paginado.
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<TradeSummaryDto>>> History(
        [FromQuery] Guid? buyerId,
        [FromQuery] Guid? sellerId,
        [FromQuery] int page = 1,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetTradeHistoryQuery(buyerId, sellerId, page), ct);
        return result.IsFailure ? BadRequest(new { error = result.Error }) : Ok(result.Value);
    }

    // Compra produto (trocar/repassar) / contrata serviço (trocar). Gate Verified. Buyer do token.
    [HttpPost("purchase")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult<string>> Purchase(
        [FromBody] PurchaseRequest request, CancellationToken ct)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new { error = "Token sem claim 'sub'." });
        }

        var result = await _mediator.Send(
            new PurchaseCommand(user.UserId, user.Nome, user.AvatarUrl, request.ListingId), ct);

        return result.IsFailure
            ? BadRequest(new { error = result.Error })
            : CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
    }

    // Serviço: comprador confirma prestação (redeem voucher). Gate Verified.
    [HttpPost("{id:guid}/redeem")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult> Redeem(Guid id, CancellationToken ct)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new { error = "Token sem claim 'sub'." });
        }

        var result = await _mediator.Send(new RedeemVoucherCommand(user.UserId, id), ct);
        return result.IsFailure ? BadRequest(new { error = result.Error }) : Ok(new { id });
    }

    // Liberação cooperativa (seller OU buyer). Gate Verified.
    [HttpPost("{id:guid}/release")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult> Release(Guid id, CancellationToken ct)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new { error = "Token sem claim 'sub'." });
        }

        var result = await _mediator.Send(new ReleaseTradeCommand(user.UserId, id), ct);
        return result.IsFailure ? BadRequest(new { error = result.Error }) : Ok(new { id });
    }

    // Abre disputa (seller OU buyer). Gate Verified.
    [HttpPost("{id:guid}/dispute")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult> Dispute(Guid id, CancellationToken ct)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new { error = "Token sem claim 'sub'." });
        }

        var result = await _mediator.Send(new OpenDisputeCommand(user.UserId, user.Nome, id), ct);
        return result.IsFailure ? BadRequest(new { error = result.Error }) : Ok(new { id });
    }

    // Árbitro/admin resolve disputa (move fundos do escrow). Gate policy "Arbitrator":
    // role ARBITRATOR (claim role no JWT) ou Admin. ResolvedBy = e-mail do token (auditoria).
    [HttpPost("{id:guid}/resolve")]
    [Authorize(Policy = "Arbitrator")]
    public async Task<ActionResult> Resolve(Guid id, [FromBody] ResolveRequest request, CancellationToken ct)
    {
        var resolvedBy = User.FindFirst("email")?.Value ?? User.FindFirst("sub")?.Value;
        var result = await _mediator.Send(new ResolveDisputeCommand(id, request.ReleaseToSeller, resolvedBy), ct);
        return result.IsFailure ? BadRequest(new { error = result.Error }) : Ok(new { id });
    }

    // Cancelamento cooperativo (seller OU buyer). Gate Verified.
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new { error = "Token sem claim 'sub'." });
        }

        var result = await _mediator.Send(new CancelTradeCommand(user.UserId, id), ct);
        return result.IsFailure ? BadRequest(new { error = result.Error }) : Ok(new { id });
    }

    // Fila de doação/voluntariado de um anúncio (anônimo vê — OOUX 13).
    [HttpGet("~/api/help")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<HelpRequestDto>>> HelpQueue(
        [FromQuery] Guid listingId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetHelpQueueQuery(listingId), ct);
        return result.IsFailure ? BadRequest(new { error = result.Error }) : Ok(result.Value);
    }

    // Pede ajuda (entra na fila de doação/voluntariado). Gate Verified. Autor do token.
    [HttpPost("~/api/help")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult<string>> RequestHelp(
        [FromBody] RequestHelpRequest request, CancellationToken ct)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new { error = "Token sem claim 'sub'." });
        }

        var result = await _mediator.Send(
            new RequestHelpCommand(user.UserId, user.Nome, user.AvatarUrl, request.ListingId, request.Mensagem), ct);

        return result.IsFailure
            ? BadRequest(new { error = result.Error })
            : CreatedAtAction(nameof(HelpQueue), new { listingId = request.ListingId }, result.Value);
    }

    // Doador seleciona receptor na fila. Gate Verified. Doador = claim sub.
    [HttpPost("~/api/help/{id:guid}/select")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult<string>> SelectRecipient(Guid id, CancellationToken ct)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new { error = "Token sem claim 'sub'." });
        }

        var result = await _mediator.Send(new SelectRecipientCommand(user.UserId, id), ct);

        return result.IsFailure
            ? BadRequest(new { error = result.Error })
            : CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
    }
}

public sealed record PurchaseRequest(Guid ListingId);

public sealed record RequestHelpRequest(Guid ListingId, string Mensagem);

public sealed record ResolveRequest(bool ReleaseToSeller);
