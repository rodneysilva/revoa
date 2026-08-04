using Revoa.Catalog.Domain.Aggregates.CommentAggregate;

namespace Revoa.Catalog.Domain.Repositories;

public interface ICommentRepository
{
    Task<Comment?> GetByIdAsync(Guid id, CancellationToken ct);

    // Raízes (parentId null) ou respostas diretas de parentId — só Visivel.
    Task<IReadOnlyList<Comment>> GetByListingAsync(Guid listingId, Guid? parentId, CancellationToken ct);

    Task AddAsync(Comment comment, CancellationToken ct);

    Task EnsureIndexesAsync(CancellationToken ct = default);
}
