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

// Vínculo usuário↔comunidade (OOUX objeto 18). UsuarioNome/AvatarUrl embed anti-N+1.
// Papel Criador é imutável (não pode ser rebaixado/bloqueado). Índice único (UsuarioId, ComunidadeId).
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
            throw new DomainException("Usuário é obrigatório.");
        }

        if (comunidadeId == Guid.Empty)
        {
            throw new DomainException("Comunidade é obrigatória.");
        }

        return new Membership
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            UsuarioNome = string.IsNullOrWhiteSpace(usuarioNome) ? "Usuário" : usuarioNome,
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
            throw new DomainException("Membro bloqueado não pode ser promovido.");
        }

        if (Papel == MembershipPapel.Criador)
        {
            throw new DomainException("Criador já é o papel máximo.");
        }

        Papel = MembershipPapel.Moderador;
        IncrementVersion();
    }

    // Apenas Moderador → Membro.
    public void RebaixarMembro()
    {
        if (Papel != MembershipPapel.Moderador)
        {
            throw new DomainException("Apenas moderadores podem ser rebaixados a membro.");
        }

        Papel = MembershipPapel.Membro;
        IncrementVersion();
    }

    public void Bloquear()
    {
        if (Papel == MembershipPapel.Criador)
        {
            throw new DomainException("Criador não pode ser bloqueado.");
        }

        if (Status == MembershipStatus.Bloqueada)
        {
            throw new DomainException("Membro já está bloqueado.");
        }

        Status = MembershipStatus.Bloqueada;
        IncrementVersion();
    }

    public void Desbloquear()
    {
        if (Status != MembershipStatus.Bloqueada)
        {
            throw new DomainException("Membro não está bloqueado.");
        }

        Status = MembershipStatus.Ativa;
        IncrementVersion();
    }
}
