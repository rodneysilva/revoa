using Revoa.Catalog.Domain.Aggregates.CommentAggregate;

namespace Revoa.Catalog.Application.DTOs;

// Mesmo shape do PostDto p/ alimentar o componente <PostThread> no frontend (reuso, sem UI duplicada).
// ChildrenCount (default 0, enriquecido em batch) permite ao FE só desenhar
// "Ver respostas" quando o comentário TEM respostas.
public sealed record CommentDto(
    Guid Id,
    Guid ListingId,
    Guid AutorId,
    string AuthorName,
    string? AutorAvatarUrl,
    string Content,
    Guid? ParentId,
    string Path,
    int Depth,
    string Status,
    DateTime CreatedAt,
    int ChildrenCount = 0);

public static class CommentDtoMapper
{
    public static CommentDto From(Comment c) => new(
        c.Id,
        c.ListingId,
        c.AutorId,
        c.AuthorName,
        c.AutorAvatarUrl,
        c.Content,
        c.ParentId,
        c.Path,
        c.Depth,
        c.Status.ToString(),
        c.CreatedAt);
}
