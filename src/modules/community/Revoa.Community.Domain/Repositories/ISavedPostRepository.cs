using Revoa.Community.Domain.Aggregates.SavedPostAggregate;

namespace Revoa.Community.Domain.Repositories;

public interface ISavedPostRepository
{
    Task<bool> ExistsAsync(Guid userId, Guid postId, CancellationToken ct);

    Task AddAsync(SavedPost saved, CancellationToken ct);

    Task RemoveAsync(Guid userId, Guid postId, CancellationToken ct);

    // Bookmarks do usuário, mais recentes primeiro (cap 200).
    Task<IReadOnlyList<SavedPost>> GetByUserAsync(Guid userId, CancellationToken ct);

    // Subconjunto de postIds salvos pelo usuário (batch — alimenta IsSaved).
    Task<IReadOnlyCollection<Guid>> GetSavedPostIdsAsync(
        Guid userId, IReadOnlyCollection<Guid> postIds, CancellationToken ct);
}
