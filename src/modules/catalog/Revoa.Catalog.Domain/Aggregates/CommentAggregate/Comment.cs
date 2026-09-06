using Revoa.Abstractions;

namespace Revoa.Catalog.Domain.Aggregates.CommentAggregate;

public enum CommentStatus
{
    Visible,
    Hidden
}

// Comentário recursivo de um anúncio (reuso do padrão de thread recursiva do Post de comunidade).
// Materialized path (depth ≤ 6). Mesmo shape do Post p/ alimentar o componente <PostThread> no FE.
// Coleção própria "Comments" (isolamento do módulo Catalog). AuthorName/AvatarUrl embed anti-N+1.
public class Comment : AggregateRoot
{
    public const int MaxDepth = 6;

    public Guid ListingId { get; private set; }
    public Guid AutorId { get; private set; }
    public string AuthorName { get; private set; } = string.Empty;
    public string? AutorAvatarUrl { get; private set; }
    public string Content { get; private set; } = string.Empty;

    public Guid? ParentId { get; private set; }
    public string Path { get; private set; } = string.Empty;
    public int Depth { get; private set; }

    public CommentStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Comment() { }

    public static Comment CreateRoot(
        Guid listingId,
        Guid autorId,
        string autorNome,
        string? autorAvatarUrl,
        string conteudo)
    {
        ValidateInvariants(listingId, autorId, conteudo);

        var id = Guid.NewGuid();
        return new Comment
        {
            Id = id,
            ListingId = listingId,
            AutorId = autorId,
            AuthorName = string.IsNullOrWhiteSpace(autorNome) ? "Usuário" : autorNome,
            AutorAvatarUrl = autorAvatarUrl,
            Content = conteudo.Trim(),
            ParentId = null,
            Path = $"/{id}/",
            Depth = 0,
            Status = CommentStatus.Visible,
            CreatedAt = DateTime.UtcNow,
            Version = 1
        };
    }

    public static Comment CreateReply(
        Comment parent,
        Guid autorId,
        string autorNome,
        string? autorAvatarUrl,
        string conteudo)
    {
        if (parent is null)
        {
            throw new DomainException("Comentário pai é obrigatório.");
        }

        if (parent.Depth >= MaxDepth)
        {
            throw new DomainException($"Profundidade máxima ({MaxDepth}) excedida — inicie um novo comentário.");
        }

        ValidateInvariants(parent.ListingId, autorId, conteudo);

        var id = Guid.NewGuid();
        return new Comment
        {
            Id = id,
            ListingId = parent.ListingId,
            AutorId = autorId,
            AuthorName = string.IsNullOrWhiteSpace(autorNome) ? "Usuário" : autorNome,
            AutorAvatarUrl = autorAvatarUrl,
            Content = conteudo.Trim(),
            ParentId = parent.Id,
            Path = parent.Path + $"{id}/",
            Depth = parent.Depth + 1,
            Status = CommentStatus.Visible,
            CreatedAt = DateTime.UtcNow,
            Version = 1
        };
    }

    private static void ValidateInvariants(Guid listingId, Guid autorId, string conteudo)
    {
        if (listingId == Guid.Empty)
        {
            throw new DomainException("Anúncio é obrigatório.");
        }

        if (autorId == Guid.Empty)
        {
            throw new DomainException("Autor é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(conteudo))
        {
            throw new DomainException("Conteúdo do comentário é obrigatório.");
        }
    }
}
