using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Revoa.Notifications.Application.Commands;
using Revoa.Notifications.Application.DTOs;
using Revoa.Notifications.Application.Queries;

namespace Revoa.Api.Controllers;

// Notificações pessoais (UF-32). Dados pessoais → SEM leitura anônima; tudo exige Verified.
// Ownership: UserId sempre do claim sub do token, nunca do body.
[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Lista notificações do usuário (50/página).
    [HttpGet]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> List(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1,
        CancellationToken ct = default)
    {
        var (userId, unauthorized) = ReadUserId();
        if (unauthorized is not null)
        {
            return Unauthorized(unauthorized);
        }

        var result = await _mediator.Send(new GetNotificationsQuery(userId, unreadOnly, page), ct);
        return result.IsFailure ? BadRequest(new { error = result.Error }) : Ok(result.Value);
    }

    // Contagem de não-lidas (badge do sino).
    [HttpGet("unread-count")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult<int>> UnreadCount(CancellationToken ct = default)
    {
        var (userId, unauthorized) = ReadUserId();
        if (unauthorized is not null)
        {
            return Unauthorized(unauthorized);
        }

        var count = await _mediator.Send(new GetUnreadCountQuery(userId), ct);
        return Ok(count);
    }

    // Marca notificação como lida (ownership: só o dono).
    [HttpPost("{id:guid}/read")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult> MarkRead(Guid id, CancellationToken ct = default)
    {
        var (userId, unauthorized) = ReadUserId();
        if (unauthorized is not null)
        {
            return Unauthorized(unauthorized);
        }

        var result = await _mediator.Send(new MarkNotificationReadCommand(userId, id), ct);
        return result.IsFailure ? BadRequest(new { error = result.Error }) : NoContent();
    }

    // Inscreve Web Push do dispositivo.
    [HttpPost("push/subscribe")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult<string>> SubscribePush(
        [FromBody] SubscribePushRequest request, CancellationToken ct = default)
    {
        var (userId, unauthorized) = ReadUserId();
        if (unauthorized is not null)
        {
            return Unauthorized(unauthorized);
        }

        var result = await _mediator.Send(
            new SubscribePushCommand(userId, request.Endpoint, request.P256dh, request.Auth), ct);

        return result.IsFailure ? BadRequest(new { error = result.Error }) : Ok(result.Value);
    }

    // Remove inscrição Web Push (logout/desinstalação). Endpoint via body ou query.
    [HttpDelete("push/subscribe")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult> UnsubscribePush(
        [FromBody] UnsubscribePushRequest? request,
        [FromQuery] string? endpoint,
        CancellationToken ct = default)
    {
        var ep = request?.Endpoint ?? endpoint;
        if (string.IsNullOrWhiteSpace(ep))
        {
            return BadRequest(new { error = "Endpoint é obrigatório." });
        }

        var result = await _mediator.Send(new UnsubscribePushCommand(ep), ct);
        return result.IsFailure ? BadRequest(new { error = result.Error }) : NoContent();
    }

    // Ownership: lê claim sub do token. Nunca do body.
    private (Guid userId, object? unauthorized) ReadUserId()
    {
        var sub = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var userId))
        {
            return (Guid.Empty, new { error = "Token sem claim 'sub'." });
        }

        return (userId, null);
    }
}

public sealed record SubscribePushRequest(string Endpoint, string P256dh, string Auth);
public sealed record UnsubscribePushRequest(string Endpoint);
