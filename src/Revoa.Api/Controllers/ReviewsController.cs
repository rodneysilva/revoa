using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Revoa.Reputation.Application.Commands;
using Revoa.Reputation.Application.DTOs;
using Revoa.Reputation.Application.Queries;

namespace Revoa.Api.Controllers;

// Avaliações pós-troca (UF-23): criação exige login+verificação + ser parte de trade Liberada;
// leitura é pública (perfil visível p/ todos). RevieweeId é derivado (contraparte do reviewer).
[ApiController]
public class ReviewsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReviewsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Cria avaliação (1–5) + comentário opcional sobre a contraparte da troca. Gate Verified.
    [HttpPost("api/trades/{id:guid}/reviews")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult<string>> Create(
        Guid id, [FromBody] CreateReviewRequest request, CancellationToken ct)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new { error = "Token sem claim 'sub'." });
        }

        var result = await _mediator.Send(
            new CreateReviewCommand(user.UserId, user.Nome, id, request.Rating, request.Comment), ct);

        return result.IsFailure
            ? BadRequest(new { error = result.Error })
            : Ok(new { id = result.Value });
    }

    // Avaliações recebidas por um usuário (perfil/ListingDetail). Leitura anônima.
    [HttpGet("api/users/{userId:guid}/reviews")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<ReviewDto>>> List(
        Guid userId, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetUserReviewsQuery(userId, limit), ct);
        return result.IsFailure ? BadRequest(new { error = result.Error }) : Ok(result.Value);
    }
}

public sealed record CreateReviewRequest(int Rating, string? Comment);
