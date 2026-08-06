using Revoa.Community.Domain.Aggregates.PostAggregate;

namespace Revoa.Community.Application.DTOs;

public sealed record PostDto(
    Guid Id,
    Guid ComunidadeId,
    Guid AutorId,
    string AutorNome,
    string? AutorAvatarUrl,
    string Conteudo,
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
        p.ComunidadeId,
        p.AutorId,
        p.AutorNome,
        p.AutorAvatarUrl,
        p.Conteudo,
        p.ParentId,
        p.Path,
        p.Depth,
        p.Status,
        p.OcultadoPor,
        p.CreatedAt,
        childrenCount);
}
