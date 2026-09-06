using Revoa.Abstractions;

namespace Revoa.Community.Domain.Aggregates.PostAggregate;

public enum PostStatus
{
    Visible,
    Hidden
}

// Post recursivo (OOUX objeto 19). Materialized path (depth â‰¤ 6). Ex.: Path "/{rootId}/{replyId}/{thisId}/".
// Cascade de ocultaÃ§Ã£o via prefix regex no Path. AuthorName/AvatarUrl embed anti-N+1.
public class Post : AggregateRoot
{
    public const int MaxDepth = 6;

    public Guid CommunityId { get; private set; }
    public Guid AutorId { get; private set; }
    public string AuthorName { get; private set; } = string.Empty;
    public string? AutorAvatarUrl { get; private set; }
    public string Content { get; private set; } = string.Empty;

    public Guid? ParentId { get; private set; }
    public string Path { get; private set; } = string.Empty;
    public int Depth { get; private set; }

    public PostStatus Status { get; private set; }
    public string? OcultadoPor { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Post() { }

    public static Post CreateRoot(
        Guid comunidadeId,
        Guid autorId,
        string autorNome,
        string? autorAvatarUrl,
        string conteudo)
    {
        ValidateInvariants(comunidadeId, autorId, conteudo);

        var id = Guid.NewGuid();
        return new Post
        {
            Id = id,
            CommunityId = comunidadeId,
            AutorId = autorId,
            AuthorName = string.IsNullOrWhiteSpace(autorNome) ? "UsuÃ¡rio" : autorNome,
            AutorAvatarUrl = autorAvatarUrl,
            Content = conteudo.Trim(),
            ParentId = null,
            Path = $"/{id}/",
            Depth = 0,
            Status = PostStatus.Visible,
            CreatedAt = DateTime.UtcNow,
            Version = 1
        };
    }

    public static Post CreateReply(
        Post parent,
        Guid autorId,
        string autorNome,
        string? autorAvatarUrl,
        string conteudo)
    {
        if (parent is null)
        {
            throw new DomainException("Post pai Ã© obrigatÃ³rio.");
        }

        if (parent.Depth >= MaxDepth)
        {
            throw new DomainException($"Profundidade mÃ¡xima ({MaxDepth}) excedida â€” inicie uma nova conversa.");
        }

        ValidateInvariants(parent.CommunityId, autorId, conteudo);

        var id = Guid.NewGuid();
        return new Post
        {
            Id = id,
            CommunityId = parent.CommunityId,
            AutorId = autorId,
            AuthorName = string.IsNullOrWhiteSpace(autorNome) ? "UsuÃ¡rio" : autorNome,
            AutorAvatarUrl = autorAvatarUrl,
            Content = conteudo.Trim(),
            ParentId = parent.Id,
            Path = parent.Path + $"{id}/",
            Depth = parent.Depth + 1,
            Status = PostStatus.Visible,
            CreatedAt = DateTime.UtcNow,
            Version = 1
        };
    }

    public void Ocultar(string ocultadoPor)
    {
        if (Status == PostStatus.Hidden)
        {
            throw new DomainException("Post jÃ¡ estÃ¡ oculto.");
        }

        Status = PostStatus.Hidden;
        OcultadoPor = string.IsNullOrWhiteSpace(ocultadoPor) ? "moderador" : ocultadoPor;
    }

    private static void ValidateInvariants(Guid comunidadeId, Guid autorId, string conteudo)
    {
        if (comunidadeId == Guid.Empty)
        {
            throw new DomainException("Comunidade Ã© obrigatÃ³ria.");
        }

        if (autorId == Guid.Empty)
        {
            throw new DomainException("Autor Ã© obrigatÃ³rio.");
        }

        if (string.IsNullOrWhiteSpace(conteudo))
        {
            throw new DomainException("ConteÃºdo do post Ã© obrigatÃ³rio.");
        }
    }
}
