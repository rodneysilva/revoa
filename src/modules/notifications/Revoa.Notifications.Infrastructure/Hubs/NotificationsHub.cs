using Microsoft.AspNetCore.SignalR;

namespace Revoa.Notifications.Infrastructure.Hubs;

// Hub de notificações pessoais (UF-32). Ao conectar, o cliente entra no grupo "user-{userId}"
// (claim sub do JWT). O Notifier faz broadcast para esse grupo. Auth via JWT em query string
// (access_token) configurada em Program.cs p/ paths /hubs.
public class NotificationsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var sub = Context.User?.FindFirst("sub")?.Value;
        if (Guid.TryParse(sub, out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }

        await base.OnConnectedAsync();
    }
}
