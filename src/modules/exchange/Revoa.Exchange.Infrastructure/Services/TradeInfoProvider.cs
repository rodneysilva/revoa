using Revoa.Exchange.Domain.Repositories;
using Revoa.IntegrationContracts.Trades;

namespace Revoa.Exchange.Infrastructure.Services;

// Adapter de ITradeInfoProvider: lê o aggregate Trade (coleção Trades) e devolve um resumo
// imutável (estado + partes) p/ o módulo Reputation — sem vazar o aggregate nem a coleção.
// Mantém o isolamento: o Reputation consome só o port (nunca o ITradeRepository).
public class TradeInfoProvider : ITradeInfoProvider
{
    private readonly ITradeRepository _trades;

    public TradeInfoProvider(ITradeRepository trades)
    {
        _trades = trades;
    }

    public async Task<TradeInfo?> GetByIdAsync(Guid tradeId, CancellationToken ct = default)
    {
        var trade = await _trades.GetByIdAsync(tradeId, ct);
        if (trade is null)
        {
            return null;
        }

        return new TradeInfo(trade.Id, trade.SellerId, trade.BuyerId, trade.State.ToString());
    }
}
