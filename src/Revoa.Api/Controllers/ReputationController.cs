using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Revoa.Abstractions;
using Revoa.Reputation.Application.DTOs;
using Revoa.Reputation.Application.Queries;

namespace Revoa.Api.Controllers;

// Score de reputação público (UF-23). Leitura anônima (perfil visível p/ todos);
// ação exige login+verificação em outros endpoints.
[ApiController]
[Route("api/users/{userId:guid}/reputation")]
[AllowAnonymous]
public class ReputationController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReputationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Score de reputação do usuário — sempre 200: sem histórico ainda devolve
    // o score zerado ("Iniciante"), pois ausência de reputação é estado válido.
    [HttpGet]
    public async Task<ActionResult<ReputationDto>> Get(Guid userId, CancellationToken ct = default)
    {
        return Ok(await _mediator.Send(new GetReputationQuery(userId), ct));
    }
}
