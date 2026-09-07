using Revoa.Community.Domain.Aggregates.PostLikeAggregate;

namespace Revoa.Community.Domain.Repositories;

public interface IPostLikeRepository
{
    Task<bool> ExistsAsync(Guid postId, Guid userId, CancellationToken ct);

    Task AddAsync(PostLike like, CancellationToken ct);

    Task RemoveAsync(Guid postId, Guid userId, CancellationToken ct);

    // Conta curtidas de cada postId informado (batch, anti-N+1).
    Task<IReadOnlyDictionary<Guid, int>> GetCountsAsync(
        IReadOnlyCollection<Guid> postIds, CancellationToken ct);

    // Subconjunto de postIds curtidos pelo usuário (batch — alimenta IsLiked).
    Task<IReadOnlyCollection<Guid>> GetLikedPostIdsAsync(
        Guid userId, IReadOnlyCollection<Guid> postIds, CancellationToken ct);

    // Todos os postIds curtidos pelo usuário (cap 500 — bootstrap do FE).
    Task<IReadOnlyList<Guid>> GetAllLikedIdsAsync(Guid userId, CancellationToken ct);
}
