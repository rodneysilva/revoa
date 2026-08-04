using MongoDB.Driver;
using Revoa.Community.Domain.Aggregates.ChatMessageAggregate;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Infrastructure.Persistence;

public class ChatMessageRepository : IChatMessageRepository
{
    private readonly IMongoCollection<ChatMessage> _messages;

    public ChatMessageRepository(IMongoDatabase database)
    {
        _messages = database.GetCollection<ChatMessage>("ChatMessages");
    }

    public async Task AddAsync(ChatMessage message, CancellationToken ct)
    {
        await _messages.InsertOneAsync(message, cancellationToken: ct);
    }

    public async Task<IReadOnlyList<ChatMessage>> GetRecentAsync(Guid comunidadeId, int limit, CancellationToken ct)
    {
        var fb = Builders<ChatMessage>.Filter;
        var query = fb.Eq(m => m.ComunidadeId, comunidadeId) & fb.Eq(m => m.Ocultado, false);

        var safeLimit = limit > 0 ? limit : 50;

        return await _messages.Find(query)
            .SortByDescending(m => m.CreatedAt)
            .Limit(safeLimit)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Índices do chat (comunidade+data desc) + TTL de 90d em CreatedAt (retenção de mensagens).
    /// </summary>
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await _messages.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<ChatMessage>(
                Builders<ChatMessage>.IndexKeys
                    .Ascending(m => m.ComunidadeId)
                    .Descending(m => m.CreatedAt),
                new CreateIndexOptions { Name = "ix_Comunidade_CreatedAt" }),
            new CreateIndexModel<ChatMessage>(
                Builders<ChatMessage>.IndexKeys.Ascending(m => m.CreatedAt),
                new CreateIndexOptions { Name = "ix_TTL_CreatedAt", ExpireAfter = TimeSpan.FromDays(90) })
        }, ct);
    }
}
