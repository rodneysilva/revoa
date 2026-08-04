using Revoa.Community.Domain.Aggregates.ChatMessageAggregate;

namespace Revoa.Community.Domain.Repositories;

public interface IChatMessageRepository
{
    Task AddAsync(ChatMessage message, CancellationToken ct);

    // Mais recentes primeiro, só não-ocultos.
    Task<IReadOnlyList<ChatMessage>> GetRecentAsync(Guid comunidadeId, int limit, CancellationToken ct);
}
