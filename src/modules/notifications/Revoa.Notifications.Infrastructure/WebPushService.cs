using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Revoa.Notifications.Application.Services;
using Revoa.Notifications.Domain.Aggregates.PushSubscriptionAggregate;
using Revoa.Notifications.Domain.Repositories;

namespace Revoa.Notifications.Infrastructure;

// Envio Web Push real (RFC 8030/8291/8292) com a crypto NATIVA do .NET — ver WebPushCrypto.
// Sem chaves VAPID configuradas o serviço fica desativado (só in-app SignalR), que é o
// comportamento de dev. Por inscrição: POST criptografado ao push service com Authorization
// VAPID; 404/410 = inscrição expirada → remove; qualquer outra falha é isolada (uma
// inscrição ruim nunca derruba as demais — quem chama é o Notifier, entrega primária SignalR).
public class WebPushService : IWebPushSender
{
    private readonly VapidOptions _vapid;
    private readonly IPushSubscriptionRepository _subscriptions;
    private readonly HttpClient _http;
    private readonly ILogger<WebPushService> _logger;

    public WebPushService(
        IOptions<VapidOptions> vapid,
        IPushSubscriptionRepository subscriptions,
        HttpClient http,
        ILogger<WebPushService> logger)
    {
        _vapid = vapid.Value;
        _subscriptions = subscriptions;
        _http = http;
        _logger = logger;
    }

    public async Task SendAsync(
        IReadOnlyList<PushSubscription> subscriptions,
        string title,
        string body,
        string? payload,
        CancellationToken ct = default)
    {
        if (subscriptions.Count == 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_vapid.PublicKey) || string.IsNullOrWhiteSpace(_vapid.PrivateKey))
        {
            _logger.LogDebug(
                "Web Push desativado (sem chaves VAPID) — {Count} subscription(ões) não enviadas.",
                subscriptions.Count);
            return;
        }

        byte[] privateKey;
        byte[] publicKey;
        try
        {
            privateKey = WebPushCrypto.Base64UrlDecode(_vapid.PrivateKey);
            publicKey = WebPushCrypto.Base64UrlDecode(_vapid.PublicKey);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Web Push: chaves VAPID não estão em base64url — envio abortado.");
            return;
        }

        // O par tem que bater: k= do Authorization usa a PublicKey da env, a assinatura sai da
        // PrivateKey — se divergirem o push service rejeita tudo com um erro difícil de rastrear.
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportECPrivateKey(WebPushCrypto.ToSec1PrivateKey(privateKey), out _);
        if (!WebPushCrypto.ExportUncompressedAsP256(ecdsa).AsSpan().SequenceEqual(publicKey))
        {
            _logger.LogError("Web Push: PublicKey VAPID não corresponde à PrivateKey — envio abortado.");
            return;
        }

        var subject = NormalizeSubject();
        var message = JsonSerializer.SerializeToUtf8Bytes(new { title, body, payload });

        foreach (var sub in subscriptions)
        {
            try
            {
                byte[] uaPublicKey;
                byte[] authSecret;
                try
                {
                    uaPublicKey = WebPushCrypto.Base64UrlDecode(sub.P256dh);
                    authSecret = WebPushCrypto.Base64UrlDecode(sub.Auth);
                }
                catch (FormatException)
                {
                    // Inscrição corrompida no banco — mesma política do 404/410: remove.
                    await _subscriptions.DeleteByEndpointAsync(sub.Endpoint, ct);
                    continue;
                }

                var encrypted = WebPushCrypto.EncryptPayload(uaPublicKey, authSecret, message);
                var authorization = WebPushCrypto.BuildVapidAuthorization(
                    new Uri(sub.Endpoint), subject, privateKey, _vapid.PublicKey);

                using var request = new HttpRequestMessage(HttpMethod.Post, sub.Endpoint)
                {
                    Content = new ByteArrayContent(encrypted)
                };
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                request.Content.Headers.TryAddWithoutValidation("Content-Encoding", "aes128gcm");
                request.Headers.TryAddWithoutValidation("Authorization", authorization);
                request.Headers.TryAddWithoutValidation("TTL", "86400");
                request.Headers.TryAddWithoutValidation("Urgency", "normal");

                using var response = await _http.SendAsync(request, ct);
                if (response.IsSuccessStatusCode)
                {
                    continue;
                }

                if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
                {
                    await _subscriptions.DeleteByEndpointAsync(sub.Endpoint, ct);
                    continue;
                }

                _logger.LogWarning(
                    "Web Push p/ {Endpoint} retornou {Status} — mensagem descartada.",
                    sub.Endpoint, (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha isolada no Web Push p/ {Endpoint}.", sub.Endpoint);
            }
        }
    }

    // RFC 8292 §3: sub deve ser mailto: ou URL — aceita valor cru e normaliza.
    private string NormalizeSubject()
    {
        var subject = _vapid.Subject?.Trim();
        if (string.IsNullOrWhiteSpace(subject))
        {
            return "mailto:dev@revoa.me";
        }

        return subject.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
               || subject.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
               || subject.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? subject
            : "mailto:" + subject;
    }
}
