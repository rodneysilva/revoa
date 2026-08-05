using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Revoa.Pricing.Application.Commands;
using Revoa.Pricing.Application.DTOs;
using Revoa.Pricing.Application.Queries;

namespace Revoa.Api.Controllers;

// Pricing Intelligence (UF-28, Fase 3): referência de preço justo por categoria. Refresh é gated
// Admin (semanal/m anual); leitura é anônima (transparência — vai alimentar revoa.org depois).
[ApiController]
[Route("api/pricing")]
public class PricingController : ControllerBase
{
    private readonly IMediator _mediator;

    public PricingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Recalcula referências de preço (mediana comunitária + BRL seed + IPCA IBGE + Ollama).
    [HttpPost("refresh")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult> Refresh(CancellationToken ct)
    {
        var result = await _mediator.Send(new RefreshPricingCommand(), ct);
        return result.IsFailure
            ? BadRequest(new { error = result.Error })
            : Ok(new { updated = result.Value });
    }

    // Referência de UMA categoria (leitura anônima). 404 se inexistente.
    [HttpGet("categories/{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<PriceReferenceDto>> GetByCategory(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPriceReferenceQuery(id), ct);
        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return result.Value is null
            ? NotFound(new { error = "Referência de preço não encontrada para esta categoria." })
            : Ok(result.Value);
    }

    // Todas as referências (leitura anônima — transparência). Ordenado por updatedAt desc.
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<PriceReferenceDto>>> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAllPriceReferencesQuery(), ct);
        return result.IsFailure
            ? BadRequest(new { error = result.Error })
            : Ok(result.Value);
    }
}
