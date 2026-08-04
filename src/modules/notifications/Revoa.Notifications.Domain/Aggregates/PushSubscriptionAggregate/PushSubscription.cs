using Revoa.Abstractions;

namespace Revoa.Notifications.Domain.Aggregates.PushSubscriptionAggregate;

// Inscrição Web Push por dispositivo (OOUX 21). Endpoint = URL do push service do navegador.
// Uma falha 404/410 no envio remove a inscrição. Índice único por Endpoint (chave natural).
public class PushSubscription : AggregateRoot
{
    public Guid UserId { get; private set; }
    public string Endpoint { get; private set; } = string.Empty;
    public string P256dh { get; private set; } = string.Empty;
    public string Auth { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private PushSubscription() { }

    public static PushSubscription Create(
        Guid userId,
        string endpoint,
        string p256dh,
        string auth)
    {
        ValidateInvariants(userId, endpoint, p256dh, auth);

        return new PushSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Endpoint = endpoint.Trim(),
            P256dh = p256dh,
            Auth = auth,
            CreatedAt = DateTime.UtcNow,
            Version = 1
        };
    }

    private static void ValidateInvariants(Guid userId, string endpoint, string p256dh, string auth)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("Usuário é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(endpoint)
            || !endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("Endpoint de push inválido (deve ser uma URL http/https).");
        }

        if (string.IsNullOrWhiteSpace(p256dh))
        {
            throw new DomainException("Chave P256dh é obrigatória.");
        }

        if (string.IsNullOrWhiteSpace(auth))
        {
            throw new DomainException("Auth secret é obrigatório.");
        }
    }
}
