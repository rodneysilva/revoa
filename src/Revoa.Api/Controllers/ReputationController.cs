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

    // Retorna o score de reputação do usuário ou 404 se ainda não tem.
    [HttpGet]
    public async Task<ActionResult<ReputationDto>> Get(Guid userId, CancellationToken ct = default)
    {
        var dto = await _mediator.Send(new GetReputationQuery(userId), ct);
        return dto is null ? NotFound(new ApiError("Reputação não encontrada para este usuário.")) : Ok(dto);
    }
}
