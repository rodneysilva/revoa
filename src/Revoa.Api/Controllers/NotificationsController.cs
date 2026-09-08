using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Revoa.Abstractions;
using Revoa.Notifications.Application.Commands;
using Revoa.Notifications.Application.DTOs;
using Revoa.Notifications.Application.Queries;
using Revoa.Notifications.Infrastructure;

namespace Revoa.Api.Controllers;

// Notificações pessoais (UF-32). Dados pessoais → SEM leitura anônima; tudo exige Verified.
// Ownership: UserId sempre do claim sub do token, nunca do body.
[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly VapidOptions _vapid;

    public NotificationsController(IMediator mediator, IOptions<VapidOptions> vapid)
    {
        _mediator = mediator;
        _vapid = vapid.Value;
    }

    // Chave pública VAPID do aplicativo (o browser precisa dela no pushManager.subscribe).
    // Anônima de propósito: chave pública não é segredo — e o front só oferece o botão de
    // "Ativar notificações" quando ela existe. PublicKey null = push desativado no ambiente.
    [HttpGet("push/key")]
    [AllowAnonymous]
    public ActionResult<PushKeyResponse> PushKey()
    {
        var key = _vapid.PublicKey;
        return Ok(new PushKeyResponse(string.IsNullOrWhiteSpace(key) ? null : key));
    }

    // Lista notificações do usuário (50/página).
    [HttpGet]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> List(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1,
        CancellationToken ct = default)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new ApiError("Token sem claim 'sub'."));
        }

        var result = await _mediator.Send(new GetNotificationsQuery(user.UserId, unreadOnly, page), ct);
        return result.IsFailure ? BadRequest(new ApiError(result.Error)) : Ok(result.Value);
    }

    // Contagem de não-lidas (badge do sino).
    [HttpGet("unread-count")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult<int>> UnreadCount(CancellationToken ct = default)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new ApiError("Token sem claim 'sub'."));
        }

        var count = await _mediator.Send(new GetUnreadCountQuery(user.UserId), ct);
        return Ok(count);
    }

    // Marca notificação como lida (ownership: só o dono).
    [HttpPost("{id:guid}/read")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult> MarkRead(Guid id, CancellationToken ct = default)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new ApiError("Token sem claim 'sub'."));
        }

        var result = await _mediator.Send(new MarkNotificationReadCommand(user.UserId, id), ct);
        return result.IsFailure ? BadRequest(new ApiError(result.Error)) : NoContent();
    }

    // Inscreve Web Push do dispositivo.
    [HttpPost("push/subscribe")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult<ResourceId>> SubscribePush(
        [FromBody] SubscribePushRequest request, CancellationToken ct = default)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new ApiError("Token sem claim 'sub'."));
        }

        var result = await _mediator.Send(
            new SubscribePushCommand(user.UserId, request.Endpoint, request.P256dh, request.Auth), ct);

        return result.IsFailure ? BadRequest(new ApiError(result.Error)) : Ok(new ResourceId(result.Value));
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
            return BadRequest(new ApiError("Endpoint é obrigatório."));
        }

        var result = await _mediator.Send(new UnsubscribePushCommand(ep), ct);
        return result.IsFailure ? BadRequest(new ApiError(result.Error)) : NoContent();
    }
}

public sealed record SubscribePushRequest(string Endpoint, string P256dh, string Auth);
public sealed record UnsubscribePushRequest(string Endpoint);
public sealed record PushKeyResponse(string? PublicKey);
