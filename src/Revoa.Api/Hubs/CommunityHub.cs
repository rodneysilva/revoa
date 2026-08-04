using Microsoft.AspNetCore.SignalR;
using Revoa.Abstractions;
using Revoa.Community.Domain.Aggregates.ChatMessageAggregate;
using Revoa.Community.Domain.Aggregates.MembershipAggregate;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Api.Hubs;

// Hub de chat tempo-real do módulo Community (UF-20/21). Cliente entra no grupo "comunidade-{id}"
// via query string `comunidadeId` ao conectar. Envio exige membro ativo (gate no hub).
public class CommunityHub : Hub
{
    private readonly IMembershipRepository _memberships;
    private readonly IChatMessageRepository _chats;

    public CommunityHub(IMembershipRepository memberships, IChatMessageRepository chats)
    {
        _memberships = memberships;
        _chats = chats;
    }

    public override async Task OnConnectedAsync()
    {
        var comunidadeIdStr = Context.GetHttpContext()?.Request.Query["comunidadeId"].ToString();
        if (Guid.TryParse(comunidadeIdStr, out var comunidadeId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"comunidade-{comunidadeId}");
        }

        await base.OnConnectedAsync();
    }

    // Envia mensagem à comunidade. Autenticação + membership ativo obrigatórios (gate).
    public async Task SendMessage(Guid comunidadeId, string conteudo)
    {
        if (Context.User?.Identity?.IsAuthenticated != true)
        {
            throw new HubException("Autenticação necessária.");
        }

        var sub = Context.User?.FindFirst("sub")?.Value;
        if (!Guid.TryParse(sub, out var autorId))
        {
            throw new HubException("Autenticação necessária.");
        }

        var nome = Context.User?.FindFirst("name")?.Value ?? "Usuário";
        var avatar = Context.User?.FindFirst("avatar")?.Value;

        var ct = Context.ConnectionAborted;
        var membership = await _memberships.GetByUsuarioEComunidadeAsync(autorId, comunidadeId, ct);
        if (membership is null || membership.Status != MembershipStatus.Ativa)
        {
            throw new HubException("Você não é membro ativo desta comunidade.");
        }

        ChatMessage message;
        try
        {
            message = ChatMessage.Create(comunidadeId, autorId, nome, avatar, conteudo ?? string.Empty);
        }
        catch (DomainException ex)
        {
            throw new HubException(ex.Message);
        }

        await _chats.AddAsync(message, ct);

        var dto = new
        {
            message.Id,
            message.ComunidadeId,
            message.AutorId,
            message.AutorNome,
            message.AutorAvatarUrl,
            message.Conteudo,
            message.CreatedAt
        };

        await Clients.Group($"comunidade-{comunidadeId}").SendAsync("ReceiveMessage", dto, ct);
    }
}
