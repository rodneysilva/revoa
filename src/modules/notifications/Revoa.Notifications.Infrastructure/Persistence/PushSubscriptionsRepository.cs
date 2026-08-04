using MongoDB.Driver;
using Revoa.Notifications.Domain.Aggregates.PushSubscriptionAggregate;
using Revoa.Notifications.Domain.Repositories;

namespace Revoa.Notifications.Infrastructure.Persistence;

public class PushSubscriptionsRepository : IPushSubscriptionRepository
{
    private readonly IMongoCollection<PushSubscription> _subscriptions;

    public PushSubscriptionsRepository(IMongoDatabase database)
    {
        _subscriptions = database.GetCollection<PushSubscription>("PushSubscriptions");
    }

    public async Task<IReadOnlyList<PushSubscription>> GetByUserAsync(Guid userId, CancellationToken ct)
    {
        return await _subscriptions.Find(s => s.UserId == userId)
            .SortByDescending(s => s.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<PushSubscription?> GetByEndpointAsync(string endpoint, CancellationToken ct)
    {
        return await _subscriptions.Find(s => s.Endpoint == endpoint).FirstOrDefaultAsync(ct);
    }

    // IsUpsert por Endpoint: insere se Endpoint novo, senão substitui o documento (atualiza UserId).
    public async Task UpsertAsync(PushSubscription subscription, CancellationToken ct)
    {
        var filter = Builders<PushSubscription>.Filter.Eq(s => s.Endpoint, subscription.Endpoint);
        await _subscriptions.ReplaceOneAsync(
            filter, subscription, new ReplaceOptions { IsUpsert = true }, ct);
    }

    public async Task DeleteByEndpointAsync(string endpoint, CancellationToken ct)
    {
        await _subscriptions.DeleteOneAsync(s => s.Endpoint == endpoint, ct);
    }

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await _subscriptions.Indexes.CreateManyAsync(new[]
        {
            // Endpoint único (chave natural do dispositivo no push service).
            new CreateIndexModel<PushSubscription>(
                Builders<PushSubscription>.IndexKeys.Ascending(s => s.Endpoint),
                new CreateIndexOptions { Name = "ux_Endpoint", Unique = true }),
            new CreateIndexModel<PushSubscription>(
                Builders<PushSubscription>.IndexKeys.Ascending(s => s.UserId),
                new CreateIndexOptions { Name = "ix_UserId" })
        }, ct);
    }
}
