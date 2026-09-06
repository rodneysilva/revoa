using MongoDB.Driver;
using Revoa.Community.Domain.Aggregates.ChatMessageAggregate;
using Revoa.Community.Domain.Repositories;
using Revoa.Infrastructure.Persistence;

namespace Revoa.Community.Infrastructure.Persistence;

public class ChatMessageRepository : MongoRepositoryBase<ChatMessage>, IChatMessageRepository, IMongoIndexEnsurer
{
    public ChatMessageRepository(IMongoDatabase database) : base(database, "ChatMessages")
    {
    }

    public async Task<IReadOnlyList<ChatMessage>> GetRecentAsync(Guid comunidadeId, int limit, CancellationToken ct)
    {
        var fb = Builders<ChatMessage>.Filter;
        var query = fb.Eq(m => m.CommunityId, comunidadeId) & fb.Eq(m => m.Ocultado, false);

        var safeLimit = limit > 0 ? limit : 50;

        return await Collection.Find(query)
            .SortByDescending(m => m.CreatedAt)
            .Limit(safeLimit)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Índices do chat (comunidade+data desc) + TTL de 90d em CreatedAt (retenção de mensagens).
    /// </summary>
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await Collection.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<ChatMessage>(
                Builders<ChatMessage>.IndexKeys
                    .Ascending(m => m.CommunityId)
                    .Descending(m => m.CreatedAt),
                new CreateIndexOptions { Name = "ix_Comunidade_CreatedAt" }),
            new CreateIndexModel<ChatMessage>(
                Builders<ChatMessage>.IndexKeys.Ascending(m => m.CreatedAt),
                new CreateIndexOptions { Name = "ix_TTL_CreatedAt", ExpireAfter = TimeSpan.FromDays(90) })
        }, ct);
    }
}
