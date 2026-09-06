using Revoa.Abstractions;

namespace Revoa.Community.Domain.Aggregates.ChatMessageAggregate;

// Mensagem de chat tempo-real (OOUX objeto 20). Append-only (SignalR). TTL de 90d no repositório.
// AuthorName/AvatarUrl embed anti-N+1. Herda Entity (Id + Version), embora seja append-only.
public class ChatMessage : Entity
{
    public Guid CommunityId { get; private set; }
    public Guid AutorId { get; private set; }
    public string AuthorName { get; private set; } = string.Empty;
    public string? AutorAvatarUrl { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public bool Ocultado { get; private set; }

    private ChatMessage() { }

    public static ChatMessage Create(
        Guid comunidadeId,
        Guid autorId,
        string autorNome,
        string? autorAvatarUrl,
        string conteudo)
    {
        if (comunidadeId == Guid.Empty)
        {
            throw new DomainException("Comunidade é obrigatória.");
        }

        if (autorId == Guid.Empty)
        {
            throw new DomainException("Autor é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(conteudo))
        {
            throw new DomainException("Conteúdo da mensagem é obrigatório.");
        }

        return new ChatMessage
        {
            Id = Guid.NewGuid(),
            CommunityId = comunidadeId,
            AutorId = autorId,
            AuthorName = string.IsNullOrWhiteSpace(autorNome) ? "Usuário" : autorNome,
            AutorAvatarUrl = autorAvatarUrl,
            Content = conteudo.Trim(),
            CreatedAt = DateTime.UtcNow,
            Ocultado = false
        };
    }
}
