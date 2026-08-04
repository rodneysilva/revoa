using Revoa.Community.Domain.Aggregates.ChatMessageAggregate;

namespace Revoa.Community.Application.DTOs;

public sealed record ChatMessageDto(
    Guid Id,
    Guid ComunidadeId,
    Guid AutorId,
    string AutorNome,
    string? AutorAvatarUrl,
    string Conteudo,
    DateTime CreatedAt);

public static class ChatMessageDtoMapper
{
    public static ChatMessageDto From(ChatMessage m) => new(
        m.Id,
        m.ComunidadeId,
        m.AutorId,
        m.AutorNome,
        m.AutorAvatarUrl,
        m.Conteudo,
        m.CreatedAt);
}
