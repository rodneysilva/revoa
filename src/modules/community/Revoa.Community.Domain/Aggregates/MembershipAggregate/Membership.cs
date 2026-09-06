using Revoa.Abstractions;

namespace Revoa.Community.Domain.Aggregates.MembershipAggregate;

public enum MembershipRole
{
    Member,
    Moderator,
    Creator
}

public enum MembershipStatus
{
    Ativa,
    Bloqueada
}

// Vínculo usuário↔comunidade (OOUX objeto 18). UserName/AvatarUrl embed anti-N+1.
// Papel Criador é imutável (não pode ser rebaixado/bloqueado). Índice único (UsuarioId, CommunityId).
public class Membership : AggregateRoot
{
    public Guid UsuarioId { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public string? UsuarioAvatarUrl { get; private set; }

    public Guid CommunityId { get; private set; }
    public MembershipRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public MembershipStatus Status { get; private set; }

    private Membership() { }

    public static Membership Create(
        Guid usuarioId,
        string usuarioNome,
        string? usuarioAvatarUrl,
        Guid comunidadeId,
        MembershipRole papel)
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
            UserName = string.IsNullOrWhiteSpace(usuarioNome) ? "Usuário" : usuarioNome,
            UsuarioAvatarUrl = usuarioAvatarUrl,
            CommunityId = comunidadeId,
            Role = papel,
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

        if (Role == MembershipRole.Creator)
        {
            throw new DomainException("Criador já é o papel máximo.");
        }

        Role = MembershipRole.Moderator;
    }

    // Apenas Moderador → Membro.
    public void RebaixarMembro()
    {
        if (Role != MembershipRole.Moderator)
        {
            throw new DomainException("Apenas moderadores podem ser rebaixados a membro.");
        }

        Role = MembershipRole.Member;
    }

    public void Bloquear()
    {
        if (Role == MembershipRole.Creator)
        {
            throw new DomainException("Criador não pode ser bloqueado.");
        }

        if (Status == MembershipStatus.Bloqueada)
        {
            throw new DomainException("Membro já está bloqueado.");
        }

        Status = MembershipStatus.Bloqueada;
    }

    public void Desbloquear()
    {
        if (Status != MembershipStatus.Bloqueada)
        {
            throw new DomainException("Membro não está bloqueado.");
        }

        Status = MembershipStatus.Ativa;
    }
}
