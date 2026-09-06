using Revoa.Abstractions;

namespace Revoa.Notifications.Domain.Aggregates.NotificationAggregate;

// Tipo de notificaÃ§Ã£o (OOUX 21). Mapeia triggers: escrow/oferta/transfer/post/chat/doaÃ§Ã£o/preÃ§o/ajuda.
public enum NotificationType
{
    EscrowUpdate,
    Offer,
    Transfer,
    Post,
    Chat,
    Donation,
    Price,
    Help,
    System
}

// NotificaÃ§Ã£o pessoal de um usuÃ¡rio (OOUX objeto 21). Payload = JSON p/ deep link (ex.: tradeId).
// Leitura/marcar-leitura exige ownership (claim sub). Lida=false ao criar.
public class Notification : AggregateRoot
{
    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Corpo { get; private set; } = string.Empty;
    public string? Payload { get; private set; }
    public bool Lida { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Notification() { }

    public static Notification Create(
        Guid userId,
        NotificationType type,
        string titulo,
        string corpo,
        string? payload)
    {
        ValidateInvariants(userId, titulo);

        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = titulo.Trim(),
            Corpo = corpo,
            Payload = payload,
            Lida = false,
            CreatedAt = DateTime.UtcNow,
            Version = 1
        };
    }

    public void MarkRead()
    {
        if (Lida)
        {
            throw new DomainException("NotificaÃ§Ã£o jÃ¡ foi marcada como lida.");
        }

        Lida = true;
        ReadAt = DateTime.UtcNow;
    }

    private static void ValidateInvariants(Guid userId, string titulo)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("UsuÃ¡rio Ã© obrigatÃ³rio.");
        }

        if (string.IsNullOrWhiteSpace(titulo))
        {
            throw new DomainException("TÃ­tulo da notificaÃ§Ã£o Ã© obrigatÃ³rio.");
        }
    }
}
