using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Revoa.Abstractions;
using Revoa.Community.Application.Commands;
using Revoa.Community.Application.DTOs;
using Revoa.Community.Application.Queries;
using Revoa.Community.Domain.Aggregates.CommunityAggregate;

namespace Revoa.Api.Controllers;

[ApiController]
[Route("api/communities")]
public class CommunityController : ControllerBase
{
    private readonly IMediator _mediator;

    public CommunityController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Feed de comunidades públicas (anônimo vê — UF-18).
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<CommunityDto>>> Feed(
        [FromQuery] double? radius,
        [FromQuery] double? lat,
        [FromQuery] double? lng,
        [FromQuery] string? axis,
        [FromQuery] int page = 1,
        CancellationToken ct = default)
    {
        CommunityAxis? eixoEnum = null;
        if (!string.IsNullOrWhiteSpace(axis) && TryParse(axis, out CommunityAxis e))
        {
            eixoEnum = e;
        }

        var result = await _mediator.Send(new GetCommunitiesQuery(radius, lat, lng, eixoEnum, page), ct);
        return result.IsFailure ? BadRequest(new ApiError(result.Error)) : Ok(result.Value);
    }

    // Detalhe de comunidade (anônimo vê).
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<CommunityDto>> Detail(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCommunityDetailQuery(id), ct);
        return result.IsFailure ? NotFound(new ApiError(result.Error)) : Ok(result.Value);
    }

    // Cria comunidade (gate Verified). Ownership (criador) do token, nunca do body.
    [HttpPost]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult<ResourceId>> Create(
        [FromBody] CreateCommunityRequest request, CancellationToken ct)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new ApiError("Token sem claim 'sub'."));
        }

        if (!TryParse(request.Type, out CommunityType tipo))
        {
            return BadRequest(new ApiError("Tipo inválido (Default|User)."));
        }

        if (!TryParse(request.Axis, out CommunityAxis axis))
        {
            return BadRequest(new ApiError("Eixo inválido (Geo|Interest|Cause)."));
        }

        if (!TryParse(request.Visibility, out CommunityVisibility visibilidade))
        {
            return BadRequest(new ApiError("Visibilidade inválida (Open|Private)."));
        }

        var command = new CreateCommunityCommand(
            user.UserId, user.Name, user.AvatarUrl,
            request.Name, request.Description, tipo, axis, visibilidade, request.Password,
            request.Lat, request.Lng, request.Neighborhood, request.City, request.State);

        var result = await _mediator.Send(command, ct);
        if (result.IsFailure)
        {
            return BadRequest(new ApiError(result.Error));
        }

        return CreatedAtAction(nameof(Detail), new ResourceId(result.Value), new ResourceId(result.Value));
    }

    // Entra em comunidade (gate Verified).
    [HttpPost("{id:guid}/join")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult> Join(Guid id, [FromBody] JoinCommunityRequest? request, CancellationToken ct)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new ApiError("Token sem claim 'sub'."));
        }

        var result = await _mediator.Send(
            new JoinCommunityCommand(user.UserId, user.Name, user.AvatarUrl, id, request?.Password), ct);

        return result.IsFailure ? BadRequest(new ApiError(result.Error)) : Ok(new ResourceId(id));
    }

    // Sai de comunidade (gate Verified).
    [HttpPost("{id:guid}/leave")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult> Leave(Guid id, CancellationToken ct)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new ApiError("Token sem claim 'sub'."));
        }

        var result = await _mediator.Send(new LeaveCommunityCommand(user.UserId, id), ct);
        return result.IsFailure ? BadRequest(new ApiError(result.Error)) : NoContent();
    }

    // Posts de uma comunidade (anônimo vê — UF-19).
    [HttpGet("{id:guid}/posts")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<PostDto>>> Posts(
        Guid id,
        [FromQuery] Guid? parentId,
        [FromQuery] int page = 1,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCommunityPostsQuery(id, parentId, page), ct);
        return result.IsFailure ? BadRequest(new ApiError(result.Error)) : Ok(result.Value);
    }

    // Cria post (gate Verified). Autor do token.
    [HttpPost("{id:guid}/posts")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult<ResourceId>> CreatePost(
        Guid id, [FromBody] CreatePostRequest request, CancellationToken ct)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new ApiError("Token sem claim 'sub'."));
        }

        var result = await _mediator.Send(
            new CreatePostCommand(user.UserId, user.Name, user.AvatarUrl, id, request.ParentId, request.Content), ct);

        if (result.IsFailure)
        {
            return BadRequest(new ApiError(result.Error));
        }

        return Ok(new ResourceId(result.Value));
    }

    // Oculta post em cascata (gate Verified; Moderador/Criador validado no handler).
    [HttpPost("{id:guid}/posts/{postId:guid}/hide")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult> HidePost(Guid id, Guid postId, CancellationToken ct)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new ApiError("Token sem claim 'sub'."));
        }

        var result = await _mediator.Send(new HidePostCommand(postId, user.Name, user.UserId), ct);
        return result.IsFailure ? BadRequest(new ApiError(result.Error)) : NoContent();
    }

    // Membros de uma comunidade (anônimo vê).
    [HttpGet("{id:guid}/members")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<MembershipDto>>> Members(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMembersQuery(id), ct);
        return result.IsFailure ? BadRequest(new ApiError(result.Error)) : Ok(result.Value);
    }

    private static bool TryParse<T>(string? value, out T result) where T : struct, Enum
        => Enum.TryParse(value, ignoreCase: true, out result);
}

public sealed record CreateCommunityRequest(
    string Name,
    string Description,
    string Type,
    string Axis,
    string Visibility,
    string? Password,
    double? Lat,
    double? Lng,
    string? Neighborhood,
    string? City,
    string? State);

public sealed record CreatePostRequest(Guid? ParentId, string Content);

public sealed record JoinCommunityRequest(string? Password);
