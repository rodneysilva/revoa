using Revoa.Abstractions;

namespace Revoa.Community.Domain.Aggregates.MembershipAggregate;

public enum MembershipPapel
{
    Membro,
    Moderador,
    Criador
}

public enum MembershipStatus
{
    Ativa,
    Bloqueada
}

// VÃ­nculo usuÃ¡rioâ†”comunidade (OOUX objeto 18). UsuarioNome/AvatarUrl embed anti-N+1.
// Papel Criador Ã© imutÃ¡vel (nÃ£o pode ser rebaixado/bloqueado). Ãndice Ãºnico (UsuarioId, ComunidadeId).
public class Membership : AggregateRoot
{
    public Guid UsuarioId { get; private set; }
    public string UsuarioNome { get; private set; } = string.Empty;
    public string? UsuarioAvatarUrl { get; private set; }

    public Guid ComunidadeId { get; private set; }
    public MembershipPapel Papel { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public MembershipStatus Status { get; private set; }

    private Membership() { }

    public static Membership Create(
        Guid usuarioId,
        string usuarioNome,
        string? usuarioAvatarUrl,
        Guid comunidadeId,
        MembershipPapel papel)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new DomainException("UsuÃ¡rio Ã© obrigatÃ³rio.");
        }

        if (comunidadeId == Guid.Empty)
        {
            throw new DomainException("Comunidade Ã© obrigatÃ³ria.");
        }

        return new Membership
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            UsuarioNome = string.IsNullOrWhiteSpace(usuarioNome) ? "UsuÃ¡rio" : usuarioNome,
            UsuarioAvatarUrl = usuarioAvatarUrl,
            ComunidadeId = comunidadeId,
            Papel = papel,
            JoinedAt = DateTime.UtcNow,
            Status = MembershipStatus.Ativa,
            Version = 1
        };
    }

    public void PromoverModerador()
    {
        if (Status != MembershipStatus.Ativa)
        {
            throw new DomainException("Membro bloqueado nÃ£o pode ser promovido.");
        }

        if (Papel == MembershipPapel.Criador)
        {
            throw new DomainException("Criador jÃ¡ Ã© o papel mÃ¡ximo.");
        }

        Papel = MembershipPapel.Moderador;
    }

    // Apenas Moderador â†’ Membro.
    public void RebaixarMembro()
    {
        if (Papel != MembershipPapel.Moderador)
        {
            throw new DomainException("Apenas moderadores podem ser rebaixados a membro.");
        }

        Papel = MembershipPapel.Membro;
    }

    public void Bloquear()
    {
        if (Papel == MembershipPapel.Criador)
        {
            throw new DomainException("Criador nÃ£o pode ser bloqueado.");
        }

        if (Status == MembershipStatus.Bloqueada)
        {
            throw new DomainException("Membro jÃ¡ estÃ¡ bloqueado.");
        }

        Status = MembershipStatus.Bloqueada;
    }

    public void Desbloquear()
    {
        if (Status != MembershipStatus.Bloqueada)
        {
            throw new DomainException("Membro nÃ£o estÃ¡ bloqueado.");
        }

        Status = MembershipStatus.Ativa;
    }
}
