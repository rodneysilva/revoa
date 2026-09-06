using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Revoa.Abstractions;
using Revoa.IntegrationContracts.Admin;
using Revoa.Pricing.Application.Commands;
using Revoa.Pricing.Application.DTOs;
using Revoa.Pricing.Application.Options;
using Revoa.Pricing.Application.Queries;

namespace Revoa.Api.Controllers;

// Pricing Intelligence (UF-28, Fase 3): referência de preço justo por categoria. Refresh é gated
// Admin (semanal/m anual); leitura é anônima (transparência — vai alimentar revoa.org depois).
[ApiController]
[Route("api/pricing")]
public class PricingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IParameterStore _params;
    private readonly PricingOptions _options;

    public PricingController(IMediator mediator, IParameterStore params_, IOptions<PricingOptions> options)
    {
        _mediator = mediator;
        _params = params_;
        _options = options.Value;
    }

    // Taxa BRL↔RVM atual (anônima). Estimativa SIMBÓLICA (RVM não é ativo financeiro) p/ dar noção
    // de valor nas trocas — a economia é de ajuda mútua; o valor é referência, não preço real.
    [HttpGet("rate")]
    [AllowAnonymous]
    public async Task<ActionResult> GetRate(CancellationToken ct)
    {
        var brlRate = await _params.GetAsync("Pricing.BrlRate", _options.BrlRate, ct);
        return Ok(new BrlRateDto(
            brlRate,
            "BRL",
            "Estimativa simbólica para trocas — o RVM é crédito de troca da comunidade, não moeda/ativo financeiro."));
    }

    // Recalcula referências de preço (mediana comunitária + BRL seed + IPCA IBGE + Ollama).
    [HttpPost("refresh")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult> Refresh(CancellationToken ct)
    {
        var result = await _mediator.Send(new RefreshPricingCommand(), ct);
        return result.IsFailure
            ? BadRequest(new ApiError(result.Error))
            : Ok(new RefreshPricingResultDto(result.Value));
    }

    // Referência de UMA categoria (leitura anônima). 404 se inexistente.
    [HttpGet("categories/{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<PriceReferenceDto>> GetByCategory(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPriceReferenceQuery(id), ct);
        if (result.IsFailure)
        {
            return BadRequest(new ApiError(result.Error));
        }

        return result.Value is null
            ? NotFound(new ApiError("Referência de preço não encontrada para esta categoria."))
            : Ok(result.Value);
    }

    // Todas as referências (leitura anônima — transparência). Ordenado por updatedAt desc.
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<PriceReferenceDto>>> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAllPriceReferencesQuery(), ct);
        return result.IsFailure
            ? BadRequest(new ApiError(result.Error))
            : Ok(result.Value);
    }
}
