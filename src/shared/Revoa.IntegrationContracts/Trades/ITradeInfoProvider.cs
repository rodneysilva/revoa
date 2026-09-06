namespace Revoa.IntegrationContracts.Trades;

// Porta (anti-corruption): permite que o módulo Reputation leia o contexto de uma troca (estado
// + partes) SEM acessar a coleção Trades (isolamento de módulos: "nunca importar repository de
// outro módulo"). O adapter vive em Revoa.Exchange.Infrastructure (lê o aggregate Trade).
// Tipos primitivos (string p/ State) para não acoplar enums do Exchange neste contrato compartilhado.
public sealed record TradeInfo(
    Guid Id,
    Guid SellerId,
    Guid BuyerId,
    string State); // "Offered" | "Funded" | "Released" | "Disputed" | "Refunded" | "Cancelled"

public interface ITradeInfoProvider
{
    Task<TradeInfo?> GetByIdAsync(Guid tradeId, CancellationToken ct = default);
}
