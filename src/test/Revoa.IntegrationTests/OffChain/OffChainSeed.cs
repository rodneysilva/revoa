using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Revoa.IntegrationTests.Harness;
using Revoa.Exchange.Domain.Aggregates.TradeAggregate;
using Revoa.Notifications.Domain.Aggregates.NotificationAggregate;

namespace Revoa.IntegrationTests.OffChain;

// Seed off-chain de aggregates direto no Mongo do Testcontainers. Dois casos em que a via HTTP
// exige chain real e o seed permite testar o contrato da API sem ela:
// - Trade: a criação real (purchase/select) move fundos no escrow on-chain; aqui criamos o
//   aggregate em qualquer estado para exercitar validações de estado/ownership dos endpoints.
// - Notification: só nasce de doação concluída (evento on-chain); aqui criamos direto para os
//   endpoints de listar/marcar-lida. Usa os mesmos tipos/mapeamentos que a API usa em produção.
internal static class OffChainSeed
{
    // Insere uma trade Service/Product em estado arbitrário (default: Funded). Carteiras/tx são
    // placeholders — os endpoints testados validam antes de tocar a chain.
    public static async Task<Trade> InsertTradeAsync(
        ApiFactory factory,
        Guid sellerId,
        Guid buyerId,
        TradeKind kind = TradeKind.Service,
        TradeState? finalState = null,
        long totalRvm = 10)
    {
        var trade = Trade.Create(
            Guid.NewGuid(),
            TradeMode.Trade,
            kind,
            sellerId, "0xSeedSellerWallet", "Vendedor Seed", null,
            buyerId, "0xSeedBuyerWallet", "Comprador Seed", null,
            totalRvm, "0xSeedAssetContract", 1, 1, DateTime.UtcNow, "0xSeedTx");

        if (finalState == TradeState.Released)
        {
            trade.MarkLiberada("0xSeedReleaseTx");
        }

        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        await database.GetCollection<Trade>("Trades").InsertOneAsync(trade);
        return trade;
    }

    // Insere N notificações não-lidas para o usuário (mais antiga primeiro).
    public static async Task<List<Notification>> InsertNotificationsAsync(
        ApiFactory factory,
        Guid userId,
        int count)
    {
        var items = Enumerable.Range(1, count)
            .Select(i => Notification.Create(
                userId, NotificationType.System, $"Notificação seed {i}", "Corpo seed E2E", null))
            .ToList();

        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        await database.GetCollection<Notification>("Notifications").InsertManyAsync(items);
        return items;
    }
}
