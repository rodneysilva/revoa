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
    public async Task<ActionResult<TradeDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTradeQuery(id), ct);
        return result.IsFailure ? NotFound(new { error = result.Error }) : Ok(result.Value);
    }

    // Histórico de trocas (anônimo vê). Filtro opcional por buyer/seller, paginado.
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<TradeDto>>> History(
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
        var (buyerId, nome, avatar, unauthorized) = ReadUser();
        if (unauthorized is not null)
        {
            return Unauthorized(unauthorized);
        }

        var result = await _mediator.Send(
            new PurchaseCommand(buyerId, nome, avatar, request.ListingId), ct);

        return result.IsFailure
            ? BadRequest(new { error = result.Error })
            : CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
    }

    // Serviço: comprador confirma prestação (redeem voucher). Gate Verified.
    [HttpPost("{id:guid}/redeem")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult> Redeem(Guid id, CancellationToken ct)
    {
        var (buyerId, _, _, unauthorized) = ReadUser();
        if (unauthorized is not null)
        {
            return Unauthorized(unauthorized);
        }

        var result = await _mediator.Send(new RedeemVoucherCommand(buyerId, id), ct);
        return result.IsFailure ? BadRequest(new { error = result.Error }) : Ok(new { id });
    }

    // Liberação cooperativa (seller OU buyer). Gate Verified.
    [HttpPost("{id:guid}/release")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult> Release(Guid id, CancellationToken ct)
    {
        var (actorId, _, _, unauthorized) = ReadUser();
        if (unauthorized is not null)
        {
            return Unauthorized(unauthorized);
        }

        var result = await _mediator.Send(new ReleaseTradeCommand(actorId, id), ct);
        return result.IsFailure ? BadRequest(new { error = result.Error }) : Ok(new { id });
    }

    // Abre disputa (seller OU buyer). Gate Verified.
    [HttpPost("{id:guid}/dispute")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult> Dispute(Guid id, CancellationToken ct)
    {
        var (actorId, nome, _, unauthorized) = ReadUser();
        if (unauthorized is not null)
        {
            return Unauthorized(unauthorized);
        }

        var result = await _mediator.Send(new OpenDisputeCommand(actorId, nome, id), ct);
        return result.IsFailure ? BadRequest(new { error = result.Error }) : Ok(new { id });
    }

    // Árbitro/admin resolve disputa (move fundos do escrow). Gate Admin (interino) até a policy
    // dedicada ARBITRATOR — claim role no JWT — substituir o allowlist por e-mail.
    [HttpPost("{id:guid}/resolve")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult> Resolve(Guid id, [FromBody] ResolveRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ResolveDisputeCommand(id, request.ReleaseToSeller), ct);
        return result.IsFailure ? BadRequest(new { error = result.Error }) : Ok(new { id });
    }

    // Cancelamento cooperativo (seller OU buyer). Gate Verified.
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var (actorId, _, _, unauthorized) = ReadUser();
        if (unauthorized is not null)
        {
            return Unauthorized(unauthorized);
        }

        var result = await _mediator.Send(new CancelTradeCommand(actorId, id), ct);
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
        var (authorId, nome, avatar, unauthorized) = ReadUser();
        if (unauthorized is not null)
        {
            return Unauthorized(unauthorized);
        }

        var result = await _mediator.Send(
            new RequestHelpCommand(authorId, nome, avatar, request.ListingId, request.Mensagem), ct);

        return result.IsFailure
            ? BadRequest(new { error = result.Error })
            : CreatedAtAction(nameof(HelpQueue), new { listingId = request.ListingId }, result.Value);
    }

    // Doador seleciona receptor na fila. Gate Verified. Doador = claim sub.
    [HttpPost("~/api/help/{id:guid}/select")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult<string>> SelectRecipient(Guid id, CancellationToken ct)
    {
        var (doadorId, _, _, unauthorized) = ReadUser();
        if (unauthorized is not null)
        {
            return Unauthorized(unauthorized);
        }

        var result = await _mediator.Send(new SelectRecipientCommand(doadorId, id), ct);

        return result.IsFailure
            ? BadRequest(new { error = result.Error })
            : CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
    }

    // Ownership: lê claims sub/name/avatar do token. Nunca do body.
    private (Guid usuarioId, string nome, string? avatar, object? unauthorized) ReadUser()
    {
        var sub = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var usuarioId))
        {
            return (Guid.Empty, "Usuário", null, new { error = "Token sem claim 'sub'." });
        }

        var nome = User.FindFirst("name")?.Value
                   ?? User.FindFirst("nickname")?.Value
                   ?? "Usuário";
        var avatar = User.FindFirst("avatar")?.Value;

        return (usuarioId, nome, avatar, null);
    }
}

public sealed record PurchaseRequest(Guid ListingId);

public sealed record RequestHelpRequest(Guid ListingId, string Mensagem);

public sealed record ResolveRequest(bool ReleaseToSeller);
