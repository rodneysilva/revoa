using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Revoa.Catalog.Application.DTOs;
using Revoa.Catalog.Application.Queries;
using Revoa.Community.Application.DTOs;
using Revoa.Community.Application.Queries;

namespace Revoa.Api.Controllers;

// Busca global (header): anúncios + comunidades em um pedido. O controller só
// orquestra as queries dos dois módulos — cada um busca no seu próprio agregado
// e aqui apenas agrupamos (mesma filosofia do UsersController).
[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private const int MaxResultados = 5;

    private readonly IMediator _mediator;

    public SearchController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public sealed record SearchResultsDto(
        IReadOnlyList<FeedItemDto> Listings,
        IReadOnlyList<CommunityDto> Communities);

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<SearchResultsDto>> Global(
        [FromQuery] string? q, CancellationToken ct)
    {
        var termo = q?.Trim();
        if (string.IsNullOrEmpty(termo) || termo.Length < 2)
        {
            return Ok(new SearchResultsDto(
                Array.Empty<FeedItemDto>(), Array.Empty<CommunityDto>()));
        }

        var feedTask = _mediator.Send(new GetFeedQuery(
            Radius: null, Lat: null, Lng: null,
            Kind: null, CategoryId: null, CommunityId: null,
            Page: 1, Mode: null, PriceMin: null, PriceMax: null,
            DonationOnly: null, Sort: null, Q: termo, SellerIds: null), ct);

        var communitiesTask = _mediator.Send(new GetCommunitiesQuery(
            Radius: null, Lat: null, Lng: null, Axis: null, Page: 1, Q: termo), ct);

        await Task.WhenAll(feedTask, communitiesTask);

        var feed = feedTask.Result;
        var communities = communitiesTask.Result;

        return Ok(new SearchResultsDto(
            feed.IsSuccess ? Take(feed.Value) : Array.Empty<FeedItemDto>(),
            communities.IsSuccess ? Take(communities.Value) : Array.Empty<CommunityDto>()));

        static IReadOnlyList<T> Take<T>(IReadOnlyList<T> list) =>
            list.Count <= MaxResultados ? list : list.Take(MaxResultados).ToList();
    }
}
