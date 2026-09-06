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
    public async Task<ActionResult<ResourceId>> Create(
        [FromBody] CreateListingRequest request, CancellationToken ct)
    {
        // Ownership: SellerId/Nome/Avatar vêm do token (claim sub + name), NUNCA do body.
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new ApiError("Token sem claim 'sub'."));
        }

        var vendedorGuid = user.UserId;
        var vendedorNome = user.Name;
        var avatar = user.AvatarUrl;

        if (!TryParse(request.Kind, out ListingKind kind))
        {
            return BadRequest(new ApiError("Kind inválido (Product|Service)."));
        }

        if (!TryParse(request.Mode, out ListingMode mode))
        {
            return BadRequest(new ApiError("Modo inválido (Trade|Resell|Donate|Volunteer)."));
        }

        if (!TryParse(request.Visibility, out ListingVisibility visibilidade))
        {
            return BadRequest(new ApiError("Visibilidade inválida (Community|Global|Both)."));
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
            kind, mode, request.Title, request.Description, request.Imagens, request.PriceRvm,
            vendedorGuid, vendedorNome, avatar,
            request.Lat, request.Lng, request.Neighborhood, request.City, request.PostalCode,
            request.CategoryId, request.CommunityId, visibilidade,
            condition, request.Stock, unitType, request.Duration, request.VoucherExpiryDays);

        var result = await _mediator.Send(command, ct);
        if (result.IsFailure)
        {
            return BadRequest(new ApiError(result.Error));
        }

        return CreatedAtAction(nameof(GetById), new ResourceId(result.Value), new ResourceId(result.Value));
    }

    // Feed anônimo (UF-01). Filtros NxN (todos opcionais) via query string.
    [HttpGet("feed")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<FeedItemDto>>> Feed(
        [FromQuery] double? radius,
        [FromQuery] double? lat,
        [FromQuery] double? lng,
        [FromQuery] string? kind,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? communityId,
        [FromQuery] int page = 1,
        [FromQuery] string? mode = null,
        [FromQuery] long? priceMin = null,
        [FromQuery] long? priceMax = null,
        [FromQuery] bool? donationOnly = null,
        [FromQuery] string? sort = null,
        [FromQuery] string? q = null,
        [FromQuery] string? sellerIds = null,
        CancellationToken ct = default)
    {
            IReadOnlyList<Guid>? vendedorGuids = null;
            if (!string.IsNullOrWhiteSpace(sellerIds))
            {
                // Endpoint público/anônimo: parse defensivo (ignora tokens inválidos em vez de
                // lançar FormatException → 500) + cap anti-abuso da cláusula $in do MongoDB.
                vendedorGuids = sellerIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(t => Guid.TryParse(t, out var g) ? g : (Guid?)null)
                    .Where(g => g.HasValue)
                    .Select(g => g!.Value)
                    .Distinct()
                    .Take(100)
                    .ToList();
            }

        var result = await _mediator.Send(
            new GetFeedQuery(radius, lat, lng, kind, categoryId, communityId, page,
                mode, priceMin, priceMax, donationOnly, sort, q, vendedorGuids),
            ct);
        return result.IsFailure ? BadRequest(new ApiError(result.Error)) : Ok(result.Value);
    }

    // Detalhe anônimo (UF-01).
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ListingDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetListingQuery(id), ct);
        return result.IsFailure ? NotFound(new ApiError(result.Error)) : Ok(result.Value);
    }

    // Salvar/dessalvar anúncio (bookmark pessoal — toggle idempotente). Login.
    [HttpPost("{id:guid}/save")]
    [Authorize]
    public async Task<ActionResult> ToggleSave(Guid id, CancellationToken ct)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new ApiError("Token sem claim 'sub'."));
        }

        var result = await _mediator.Send(new ToggleSaveListingCommand(user.UserId, id), ct);
        return result.IsFailure
            ? NotFound(new ApiError(result.Error))
            : Ok(new { Saved = result.Value });
    }

    // Anúncios salvos do usuário do token (cards prontos). Privado.
    [HttpGet("saved")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<FeedItemDto>>> Saved(CancellationToken ct)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new ApiError("Token sem claim 'sub'."));
        }

        var result = await _mediator.Send(new GetSavedListingsQuery(user.UserId), ct);
        return result.IsFailure ? BadRequest(new ApiError(result.Error)) : Ok(result.Value);
    }

    // Ids dos anúncios salvos (estado inicial do botão salvar no FE). Privado.
    [HttpGet("saved/ids")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<Guid>>> SavedIds(CancellationToken ct)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new ApiError("Token sem claim 'sub'."));
        }

        var ids = await _mediator.Send(new GetSavedListingIdsQuery(user.UserId), ct);
        return Ok(ids);
    }

    // Categorias ativas (dropdown do Criar Anúncio + filtros). Anônimo.
    [HttpGet("/api/categories")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> Categories(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(), ct);
        return result.IsFailure ? BadRequest(new ApiError(result.Error)) : Ok(result.Value);
    }

    // Comentários de um anúncio (thread recursiva — reuso do <PostThread> no FE). Raízes ou
    // respostas diretas de parentId. Anônimo vê (UF-01).
    [HttpGet("{id:guid}/comments")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<CommentDto>>> Comments(
        Guid id, [FromQuery] Guid? parentId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetListingCommentsQuery(id, parentId), ct);
        return result.IsFailure ? BadRequest(new ApiError(result.Error)) : Ok(result.Value);
    }

    // Cria comentário/resposta (gate Verified). Autor do token; depth ≤ 6.
    [HttpPost("{id:guid}/comments")]
    [Authorize(Policy = "Verified")]
    public async Task<ActionResult<ResourceId>> CreateComment(
        Guid id, [FromBody] CreateCommentRequest request, CancellationToken ct)
    {
        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized(new ApiError("Token sem claim 'sub'."));
        }

        var result = await _mediator.Send(
            new CreateCommentCommand(user.UserId, user.Name, user.AvatarUrl, id, request.ParentId, request.Content), ct);

        return result.IsFailure ? BadRequest(new ApiError(result.Error)) : Ok(new ResourceId(result.Value));
    }

    private static bool TryParse<T>(string? value, out T result) where T : struct, Enum
        => Enum.TryParse(value, ignoreCase: true, out result);
}

public sealed record CreateCommentRequest(Guid? ParentId, string Content);

public sealed record CreateListingRequest(
    string Kind,
    string Mode,
    string Title,
    string Description,
    List<string> Imagens,
    long PriceRvm,
    double? Lat,
    double? Lng,
    string? Neighborhood,
    string? City,
    string? PostalCode,
    Guid CategoryId,
    Guid? CommunityId,
    string Visibility,
    string? Condition,
    int? Stock,
    string? UnitType,
    int? Duration,
    int? VoucherExpiryDays);
