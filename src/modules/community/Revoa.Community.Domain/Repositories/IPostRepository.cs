using Revoa.Community.Domain.Aggregates.PostAggregate;

namespace Revoa.Community.Domain.Repositories;

public interface IPostRepository
{
    Task<Post?> GetByIdAsync(Guid id, CancellationToken ct);

    // parentId null → posts raiz; senão filhos diretos. Só Visivel, ordenado por CreatedAt asc.
    Task<IReadOnlyList<Post>> GetByCommunityAsync(Guid communityId, Guid? parentId, CancellationToken ct);

    // Feed recente (raiz + respostas), só Visivel, ordenado por CreatedAt desc.
    Task<IReadOnlyList<Post>> GetByComunidadeRecentAsync(Guid communityId, int limit, CancellationToken ct);

    // Posts Visivel do autor em qualquer comunidade, mais recentes primeiro (perfil público).
    Task<IReadOnlyList<Post>> GetByAutorAsync(Guid autorId, int limit, CancellationToken ct);

    // Conta respostas diretas (Status Visivel) de cada parentId informado (batch, anti-N+1).
    Task<IReadOnlyDictionary<Guid, int>> GetChildrenCountsAsync(
        IReadOnlyCollection<Guid> parentIds, CancellationToken ct);

    Task AddAsync(Post post, CancellationToken ct);

    Task UpdateAsync(Post post, CancellationToken ct);

    // Oculta o post e todos os descendentes cujo Path inicia com `path` (prefix regex).
    Task HideCascadeAsync(string path, string ocultadoPor, CancellationToken ct);
}
