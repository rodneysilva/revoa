using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Revoa.Abstractions;
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
        [FromQuery] double? raio,
        [FromQuery] double? lat,
        [FromQuery] double? lng,
        [FromQuery] string? eixo,
        [FromQuery] int page = 1,
        CancellationToken ct = default)
    {
        CommunityEixo? eixoEnum = null;
        if (!string.IsNullOrWhiteSpace(eixo) && TryParse(eixo, out CommunityEixo e))
        {
            eixoEnum = e;
        }

        var result = await _mediator.Send(new GetCommunitiesQuery(raio, lat, lng, eixoEnum, page), ct);
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
    public async Task<ActionResult<string>> Create(
        [FromBody] CreateCommunityRequest request, CancellationToken ct)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new ApiError("Token sem claim 'sub'."));
        }

        if (!TryParse(request.Tipo, out CommunityTipo tipo))
        {
            return BadRequest(new ApiError("Tipo inválido (Default|User)."));
        }

        if (!TryParse(request.Eixo, out CommunityEixo eixo))
        {
            return BadRequest(new ApiError("Eixo inválido (Geo|Interesse|Causa)."));
        }

        if (!TryParse(request.Visibilidade, out CommunityVisibilidade visibilidade))
        {
            return BadRequest(new ApiError("Visibilidade inválida (Open|Private)."));
        }

        var command = new CreateCommunityCommand(
            user.UserId, user.Nome, user.AvatarUrl,
            request.Nome, request.Descricao, tipo, eixo, visibilidade, request.Password,
            request.Lat, request.Lng, request.Bairro, request.Cidade, request.Estado);

        var result = await _mediator.Send(command, ct);
        if (result.IsFailure)
        {
            return BadRequest(new ApiError(result.Error));
        }

        return CreatedAtAction(nameof(Detail), new ResourceId(result.Value), result.Value);
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
            new JoinCommunityCommand(user.UserId, user.Nome, user.AvatarUrl, id, request?.Password), ct);

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
    public async Task<ActionResult<string>> CreatePost(
        Guid id, [FromBody] CreatePostRequest request, CancellationToken ct)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new ApiError("Token sem claim 'sub'."));
        }

        var result = await _mediator.Send(
            new CreatePostCommand(user.UserId, user.Nome, user.AvatarUrl, id, request.ParentId, request.Conteudo), ct);

        if (result.IsFailure)
        {
            return BadRequest(new ApiError(result.Error));
        }

        return Ok(result.Value);
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

        var result = await _mediator.Send(new HidePostCommand(postId, user.Nome, user.UserId), ct);
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
    string Nome,
    string Descricao,
    string Tipo,
    string Eixo,
    string Visibilidade,
    string? Password,
    double? Lat,
    double? Lng,
    string? Bairro,
    string? Cidade,
    string? Estado);

public sealed record CreatePostRequest(Guid? ParentId, string Conteudo);

public sealed record JoinCommunityRequest(string? Password);
