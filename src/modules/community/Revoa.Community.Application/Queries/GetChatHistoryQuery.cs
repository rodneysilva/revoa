using MediatR;
using Revoa.Abstractions;
using Revoa.Community.Application.DTOs;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Application.Queries;

// Histórico do chat ao vivo (GET /api/communities/{id}/chat). O hub SignalR já
// persiste cada mensagem (IChatMessageRepository, expira em 90 dias) — esta query
// expõe as recentes em ordem cronológica (mais antigas primeiro) para o LiveChat
// montar o histórico antes de receber as mensagens ao vivo.
//
// Leitura pública (como posts): ver a conversa não exige vínculo — só publicar.
public sealed record GetChatHistoryQuery(Guid CommunityId, int Limit = 50)
    : IRequest<Result<IReadOnlyList<ChatMessageDto>>>;

public class GetChatHistoryQueryHandler
    : IRequestHandler<GetChatHistoryQuery, Result<IReadOnlyList<ChatMessageDto>>>
{
    private readonly ICommunityRepository _communities;
    private readonly IChatMessageRepository _chats;

    public GetChatHistoryQueryHandler(
        ICommunityRepository communities,
        IChatMessageRepository chats)
    {
        _communities = communities;
        _chats = chats;
    }

    public async Task<Result<IReadOnlyList<ChatMessageDto>>> Handle(
        GetChatHistoryQuery request, CancellationToken ct)
    {
        var community = await _communities.GetByIdAsync(request.CommunityId, ct);
        if (community is null)
        {
            return Result<IReadOnlyList<ChatMessageDto>>.Fail("Comunidade não encontrada.");
        }

        var limit = Math.Clamp(request.Limit, 1, 100);
        // Repo devolve mais recentes primeiro; a UI quer cronológico — inverte.
        var recent = await _chats.GetRecentAsync(request.CommunityId, limit, ct);
        return Result<IReadOnlyList<ChatMessageDto>>.Ok(
            recent.Reverse().Select(ChatMessageDtoMapper.From).ToList());
    }
}
