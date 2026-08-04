using MediatR;
using Revoa.Abstractions;
using Revoa.Exchange.Application.DTOs;
using Revoa.Exchange.Domain.Repositories;

namespace Revoa.Exchange.Application.Queries;

// Histórico de trocas (GET /api/trades). Filtro opcional por buyer/seller, paginado. Anônimo vê.
public sealed record GetTradeHistoryQuery(
    Guid? BuyerId,
    Guid? SellerId,
    int Page) : IRequest<Result<IReadOnlyList<TradeDto>>>;

public class GetTradeHistoryQueryHandler : IRequestHandler<GetTradeHistoryQuery, Result<IReadOnlyList<TradeDto>>>
{
    private const int PageSize = 20;

    private readonly ITradeRepository _tradeRepo;

    public GetTradeHistoryQueryHandler(ITradeRepository tradeRepo)
    {
        _tradeRepo = tradeRepo;
    }

    public async Task<Result<IReadOnlyList<TradeDto>>> Handle(GetTradeHistoryQuery request, CancellationToken ct)
    {
        var page = request.Page <= 0 ? 1 : request.Page;

        var trades = await _tradeRepo.GetHistoryAsync(request.BuyerId, request.SellerId, page, PageSize, ct);

        IReadOnlyList<TradeDto> result = trades
            .Select(TradeDtoMapper.From)
            .ToList();
        return Result<IReadOnlyList<TradeDto>>.Ok(result);
    }
}
