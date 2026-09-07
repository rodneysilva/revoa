using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Revoa.Abstractions;
using Revoa.Community.Application.Commands;
using Revoa.Community.Application.DTOs;
using Revoa.Community.Application.Queries;

namespace Revoa.Api.Controllers;

// Curtir/salvar posts e listagens pessoais. Rota distinta de /api/communities
// porque opera sobre o post em si (qualquer comunidade, sem gate de membership
// — o feed público mostra posts de comunidades que o usuário não participa).
// {postId:guid} elimina ambiguidade com os literais saved/liked.
[ApiController]
[Route("api/posts")]
public class PostsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PostsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Feed público de posts: raízes de comunidades ativas, mais recentes
    // primeiro (anônimo vê; com JWT popula IsLiked/IsSaved do viewer).
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<PublicPostItemDto>>> Feed(
        [FromQuery] int page = 1, CancellationToken ct = default)
    {
        var viewer = User.GetRevoaUser()?.UserId;

        var result = await _mediator.Send(new GetPublicPostsQuery(page, viewer), ct);
        return result.IsFailure ? BadRequest(new ApiError(result.Error)) : Ok(result.Value);
    }

    // Curtir/desscurtir (gate Verified). Retorna true se agora está curtido.
    [HttpPost("{postId:guid}/like")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult> ToggleLike(Guid postId, CancellationToken ct)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new ApiError("Token sem claim 'sub'."));
        }

        var result = await _mediator.Send(new ToggleLikePostCommand(postId, user.UserId), ct);
        return result.IsFailure
            ? NotFound(new ApiError(result.Error))
            : Ok(new LikeToggleResponse(result.Value));
    }

    // Salvar/dessalvar (gate Verified). Retorna true se agora está salvo.
    [HttpPost("{postId:guid}/save")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult> ToggleSave(Guid postId, CancellationToken ct)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new ApiError("Token sem claim 'sub'."));
        }

        var result = await _mediator.Send(new ToggleSavePostCommand(postId, user.UserId), ct);
        return result.IsFailure
            ? NotFound(new ApiError(result.Error))
            : Ok(new SaveToggleResponse(result.Value));
    }

    // Posts salvos pelo usuário autenticado (privado — UserId do token).
    [HttpGet("saved")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<PostDto>>> Saved(CancellationToken ct)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new ApiError("Token sem claim 'sub'."));
        }

        var result = await _mediator.Send(new GetSavedPostsQuery(user.UserId), ct);
        return Ok(result.Value);
    }

    // Ids de posts salvos (bootstrap do botão 🔖 no FE).
    [HttpGet("saved/ids")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<Guid>>> SavedIds(CancellationToken ct)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new ApiError("Token sem claim 'sub'."));
        }

        var ids = await _mediator.Send(new GetSavedPostIdsQuery(user.UserId), ct);
        return Ok(ids);
    }

    // Ids de posts curtidos (bootstrap do botão ❤ no FE).
    [HttpGet("liked/ids")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<Guid>>> LikedIds(CancellationToken ct)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new ApiError("Token sem claim 'sub'."));
        }

        var ids = await _mediator.Send(new GetLikedPostIdsQuery(user.UserId), ct);
        return Ok(ids);
    }
}

public sealed record LikeToggleResponse(bool Liked);

public sealed record SaveToggleResponse(bool Saved);
