using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Revoa.Abstractions;
using Revoa.Community.Application.DTOs;
using Revoa.Community.Application.Queries;
using Revoa.Identity.Application.Queries;

namespace Revoa.Api.Controllers;

// Perfil público de usuário. Reviews e reputação já moram em seus controllers
// (mesma rota pai api/users/{id}); aqui ficam o perfil em si e a atividade social.
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Dados públicos (nome + membro desde). Banido/inativo → 404.
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Profile(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPublicProfileQuery(id), ct);
        return result.IsFailure ? NotFound(new ApiError(result.Error)) : Ok(result.Value);
    }

    // Comunidades com vínculo Active do usuário.
    [HttpGet("{id:guid}/communities")]
    [AllowAnonymous]
    public async Task<IActionResult> Communities(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetUserCommunitiesQuery(id), ct);
        return result.IsFailure ? NotFound(new ApiError(result.Error)) : Ok(result.Value);
    }

    // Posts do usuário em comunidades Open (atividade pública do perfil).
    [HttpGet("{id:guid}/posts")]
    [AllowAnonymous]
    public async Task<IActionResult> Posts(Guid id, [FromQuery] int limit = 12, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetUserPostsQuery(id, limit), ct);
        return result.IsFailure ? NotFound(new ApiError(result.Error)) : Ok(result.Value);
    }
}
