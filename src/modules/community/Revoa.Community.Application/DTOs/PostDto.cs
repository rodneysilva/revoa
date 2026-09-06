using Revoa.Community.Domain.Aggregates.PostAggregate;

namespace Revoa.Community.Application.DTOs;

public sealed record PostDto(
    Guid Id,
    Guid CommunityId,
    Guid AutorId,
    string AuthorName,
    string? AutorAvatarUrl,
    string Content,
    Guid? ParentId,
    string Path,
    int Depth,
    PostStatus Status,
    string? OcultadoPor,
    DateTime CreatedAt,
    int ChildrenCount);

public static class PostDtoMapper
{
    public static PostDto From(Post p, int childrenCount = 0) => new(
        p.Id,
        p.CommunityId,
        p.AutorId,
        p.AuthorName,
        p.AutorAvatarUrl,
        p.Content,
        p.ParentId,
        p.Path,
        p.Depth,
        p.Status,
        p.OcultadoPor,
        p.CreatedAt,
        childrenCount);
}
