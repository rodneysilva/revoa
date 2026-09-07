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
    int ChildrenCount,
    int LikeCount = 0,
    bool IsLiked = false,
    bool IsSaved = false);

public static class PostDtoMapper
{
    public static PostDto From(
        Post p,
        int childrenCount = 0,
        int likeCount = 0,
        bool isLiked = false,
        bool isSaved = false) => new(
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
        childrenCount,
        likeCount,
        isLiked,
        isSaved);
}
