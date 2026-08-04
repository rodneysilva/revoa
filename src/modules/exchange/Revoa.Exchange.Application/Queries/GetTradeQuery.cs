using MediatR;
using Revoa.Abstractions;
using Revoa.Exchange.Application.DTOs;
using Revoa.Exchange.Domain.Repositories;

namespace Revoa.Exchange.Application.Queries;

// Detalhe de uma troca (GET /api/trades/{id}). Anônimo vê.
public sealed record GetTradeQuery(Guid Id) : IRequest<Result<TradeDto>>;

public class GetTradeQueryHandler : IRequestHandler<GetTradeQuery, Result<TradeDto>>
{
    private readonly ITradeRepository _tradeRepo;

    public GetTradeQueryHandler(ITradeRepository tradeRepo)
    {
        _tradeRepo = tradeRepo;
    }

    public async Task<Result<TradeDto>> Handle(GetTradeQuery request, CancellationToken ct)
    {
        var trade = await _tradeRepo.GetByIdAsync(request.Id, ct);
        if (trade is null)
        {
            return Result<TradeDto>.Fail("Troca não encontrada.");
        }

        return Result<TradeDto>.Ok(TradeDtoMapper.From(trade));
    }
}
