using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Revoa.Abstractions;
using Revoa.Catalog.Application.Commands;
using Revoa.Catalog.Application.DTOs;
using Revoa.Catalog.Application.Queries;
using Revoa.Catalog.Domain.Aggregates.ListingAggregate;

namespace Revoa.Api.Controllers;

[ApiController]
[Route("api/listings")]
public class CatalogController : ControllerBase
{
    private readonly IMediator _mediator;

    public CatalogController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Ação exige login + verificação dupla (gate UF-01: aberto p/ navegar, fechado p/ agir).
    [HttpPost]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult<string>> Create(
        [FromBody] CreateListingRequest request, CancellationToken ct)
    {
        // Ownership: VendedorId/Nome/Avatar vêm do token (claim sub + name), NUNCA do body.
        var vendedorId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(vendedorId) || !Guid.TryParse(vendedorId, out var vendedorGuid))
        {
            return Unauthorized(new { error = "Token sem claim 'sub'." });
        }

        // TODO: claim de nome/avatar será populada pelo Identity via evento/read model; enquanto
        // ausente, usamos placeholder "Usuário".
        var vendedorNome = User.FindFirst("name")?.Value
                           ?? User.FindFirst("nickname")?.Value
                           ?? "Usuário";
        var avatar = User.FindFirst("avatar")?.Value;

        if (!TryParse(request.Kind, out ListingKind kind))
        {
            return BadRequest(new { error = "Kind inválido (Product|Service)." });
        }

        if (!TryParse(request.Modo, out ListingModo modo))
        {
            return BadRequest(new { error = "Modo inválido (Trocar|Repassar|Doar|Voluntariar)." });
        }

        if (!TryParse(request.Visibilidade, out ListingVisibilidade visibilidade))
        {
            return BadRequest(new { error = "Visibilidade inválida (Comunidade|Global|Ambos)." });
        }

        ProductCondition? condition = null;
        if (request.Condition is not null && TryParse(request.Condition, out ProductCondition c))
        {
            condition = c;
        }

        ServiceUnitType? unitType = null;
        if (request.UnitType is not null && TryParse(request.UnitType, out ServiceUnitType u))
        {
            unitType = u;
        }

        var command = new CreateListingCommand(
            kind, modo, request.Titulo, request.Descricao, request.Imagens, request.PrecoRvm,
            vendedorGuid, vendedorNome, avatar,
            request.Lat, request.Lng, request.Bairro, request.Cidade, request.Cep,
            request.CategoriaId, request.ComunidadeId, visibilidade,
            condition, request.Stock, unitType, request.Duration, request.VoucherExpiryDays);

        var result = await _mediator.Send(command, ct);
        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
    }

    // Feed anônimo (UF-01).
    [HttpGet("feed")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<FeedItemDto>>> Feed(
        [FromQuery] double? raio,
        [FromQuery] double? lat,
        [FromQuery] double? lng,
        [FromQuery] string? kind,
        [FromQuery] Guid? categoriaId,
        [FromQuery] Guid? comunidadeId,
        [FromQuery] int page = 1,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetFeedQuery(raio, lat, lng, kind, categoriaId, comunidadeId, page), ct);
        return result.IsFailure ? BadRequest(new { error = result.Error }) : Ok(result.Value);
    }

    // Detalhe anônimo (UF-01).
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ListingDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetListingQuery(id), ct);
        return result.IsFailure ? NotFound(new { error = result.Error }) : Ok(result.Value);
    }

    // Categorias ativas (dropdown do Criar Anúncio + filtros). Anônimo.
    [HttpGet("/api/categories")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> Categories(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(), ct);
        return result.IsFailure ? BadRequest(new { error = result.Error }) : Ok(result.Value);
    }

    private static bool TryParse<T>(string? value, out T result) where T : struct, Enum
        => Enum.TryParse(value, ignoreCase: true, out result);
}

public sealed record CreateListingRequest(
    string Kind,
    string Modo,
    string Titulo,
    string Descricao,
    List<string> Imagens,
    long PrecoRvm,
    double? Lat,
    double? Lng,
    string? Bairro,
    string? Cidade,
    string? Cep,
    Guid CategoriaId,
    Guid? ComunidadeId,
    string Visibilidade,
    string? Condition,
    int? Stock,
    string? UnitType,
    int? Duration,
    int? VoucherExpiryDays);
