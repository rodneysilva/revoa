using Revoa.Abstractions;

namespace Revoa.Community.Domain.Aggregates.CommunityAggregate;

public enum CommunityType
{
    Default,
    User
}

public enum CommunityAxis
{
    Geo,
    Interest,
    Cause
}

public enum CommunityVisibility
{
    Open,
    Private
}

public enum CommunityStatus
{
    Active,
    Archived
}

// Comunidade (OOUX objeto 17). Default = uma por city (sempre Open, auto-vínculo no onboarding);
// User = criada por usuário (Open ou Private com senha). Criador é embed (Nome/AvatarUrl) anti-N+1.
//
// Tipo nomeado "CommunityGroup" (e não "Community") para evitar colisão com o namespace Revoa.Community
// (mesma convenção do módulo Account, que nomeia o aggregate "UserAccount" em vez de "Account").
public class CommunityGroup : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public CommunityType Type { get; private set; }
    public CommunityAxis Axis { get; private set; }
    public CommunityVisibility Visibility { get; private set; }

    // Só para Private.
    public string? PasswordHash { get; private set; }

    public double? Lat { get; private set; }
    public double? Lng { get; private set; }
    public string? Neighborhood { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }

    // Capa opcional (upload via /api/media?folder=communities). Null → gradiente no FE.
    public string? CoverImageUrl { get; private set; }

    public Guid CreatorId { get; private set; }
    public string CreatorName { get; private set; } = string.Empty;
    public string? CreatorAvatarUrl { get; private set; }

    public CommunityStatus Status { get; private set; }

    private CommunityGroup() { }

    public static CommunityGroup Create(
        string name,
        string description,
        CommunityType type,
        CommunityAxis axis,
        CommunityVisibility visibility,
        string? password,
        double? lat,
        double? lng,
        string? neighborhood,
        string? city,
        string? state,
        Guid creatorId,
        string creatorName,
        string? creatorAvatarUrl,
        string? coverImageUrl = null)
    {
        ValidateInvariants(name, type, visibility, password, creatorId);

        return new CommunityGroup
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Type = type,
            Axis = axis,
            Visibility = visibility,
            PasswordHash = null,
            Lat = lat,
            Lng = lng,
            Neighborhood = neighborhood,
            City = city,
            State = state,
            CoverImageUrl = string.IsNullOrWhiteSpace(coverImageUrl) ? null : coverImageUrl.Trim(),
            CreatorId = creatorId,
            CreatorName = string.IsNullOrWhiteSpace(creatorName) ? "Usuário" : creatorName,
            CreatorAvatarUrl = creatorAvatarUrl,
            Status = CommunityStatus.Active,
            Version = 1
        };
    }

    // Seta o hash da senha (apenas Private). Usado na criação (handler) e na rotação de senha.
    public void SetPasswordHash(string hash)
    {
        if (Visibility != CommunityVisibility.Private)
        {
            throw new DomainException("Apenas comunidades privadas possuem senha.");
        }

        PasswordHash = hash;
    }

    // Define/remove a capa (URL do /api/media). Null volta ao gradiente do FE.
    public void SetCoverImageUrl(string? coverImageUrl)
    {
        if (coverImageUrl is not null && coverImageUrl.Length > 500)
        {
            throw new DomainException("URL da capa deve ter no máximo 500 caracteres.");
        }

        CoverImageUrl = string.IsNullOrWhiteSpace(coverImageUrl) ? null : coverImageUrl.Trim();
    }

    public void Archive()
    {
        if (Status == CommunityStatus.Archived)
        {
            throw new DomainException("Comunidade já está arquivada.");
        }

        Status = CommunityStatus.Archived;
    }

    // Reativação por admin (painel): Archived → Active. Volta a aparecer nos feeds.
    public void Reactivate()
    {
        if (Status != CommunityStatus.Archived)
        {
            throw new DomainException("Comunidade não está arquivada.");
        }

        Status = CommunityStatus.Active;
    }

    private static void ValidateInvariants(
        string name,
        CommunityType type,
        CommunityVisibility visibility,
        string? password,
        Guid creatorId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Nome da comunidade é obrigatório.");
        }

        // Default (uma por city) é sempre Open.
        if (type == CommunityType.Default && visibility != CommunityVisibility.Open)
        {
            throw new DomainException("Comunidades Default são sempre Open.");
        }

        if (visibility == CommunityVisibility.Private && string.IsNullOrWhiteSpace(password))
        {
            throw new DomainException("Comunidades privadas exigem senha.");
        }

        if (creatorId == Guid.Empty)
        {
            throw new DomainException("Criador é obrigatório.");
        }
    }
}
