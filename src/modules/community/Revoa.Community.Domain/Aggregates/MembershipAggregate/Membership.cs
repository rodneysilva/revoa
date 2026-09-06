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
    Active,
    Blocked
}

// Vínculo usuário↔comunidade (OOUX objeto 18). UserName/AvatarUrl embed anti-N+1.
// Papel Criador é imutável (não pode ser rebaixado/bloqueado). Índice único (UserId, CommunityId).
public class Membership : AggregateRoot
{
    public Guid UserId { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public string? UserAvatarUrl { get; private set; }

    public Guid CommunityId { get; private set; }
    public MembershipRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public MembershipStatus Status { get; private set; }

    private Membership() { }

    public static Membership Create(
        Guid userId,
        string userName,
        string? userAvatarUrl,
        Guid communityId,
        MembershipRole papel)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("Usuário é obrigatório.");
        }

        if (communityId == Guid.Empty)
        {
            throw new DomainException("Comunidade é obrigatória.");
        }

        return new Membership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UserName = string.IsNullOrWhiteSpace(userName) ? "Usuário" : userName,
            UserAvatarUrl = userAvatarUrl,
            CommunityId = communityId,
            Role = papel,
            JoinedAt = DateTime.UtcNow,
            Status = MembershipStatus.Active,
            Version = 1
        };
    }

    public void PromoverModerador()
    {
        if (Status != MembershipStatus.Active)
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

        if (Status == MembershipStatus.Blocked)
        {
            throw new DomainException("Membro já está bloqueado.");
        }

        Status = MembershipStatus.Blocked;
    }

    public void Desbloquear()
    {
        if (Status != MembershipStatus.Blocked)
        {
            throw new DomainException("Membro não está bloqueado.");
        }

        Status = MembershipStatus.Active;
    }
}
