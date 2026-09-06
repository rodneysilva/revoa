using Revoa.Community.Domain.Aggregates.ChatMessageAggregate;

namespace Revoa.Community.Application.DTOs;

public sealed record ChatMessageDto(
    Guid Id,
    Guid CommunityId,
    Guid AutorId,
    string AuthorName,
    string? AutorAvatarUrl,
    string Content,
    DateTime CreatedAt);

public static class ChatMessageDtoMapper
{
    public static ChatMessageDto From(ChatMessage m) => new(
        m.Id,
        m.CommunityId,
        m.AutorId,
        m.AuthorName,
        m.AutorAvatarUrl,
        m.Content,
        m.CreatedAt);
}
