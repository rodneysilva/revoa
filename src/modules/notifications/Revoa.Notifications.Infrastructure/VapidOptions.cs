namespace Revoa.Notifications.Infrastructure;

// Chaves VAPID para Web Push (RFC 8291). Vazias em dev = Web Push desativado (só in-app SignalR).
// TODO: gerar reais via VapidHelper.GenerateVapidKeys() (library WebPush) e prover via secret/env.
public sealed class VapidOptions
{
    public const string SectionName = "WebPush:Vapid";

    public string Subject { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
}
