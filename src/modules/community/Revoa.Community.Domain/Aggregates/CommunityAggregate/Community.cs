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

// Comunidade (OOUX objeto 17). Default = uma por cidade (sempre Open, auto-vínculo no onboarding);
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

    public Guid CreatorId { get; private set; }
    public string CreatorName { get; private set; } = string.Empty;
    public string? CreatorAvatarUrl { get; private set; }

    public CommunityStatus Status { get; private set; }

    private CommunityGroup() { }

    public static CommunityGroup Create(
        string nome,
        string descricao,
        CommunityType tipo,
        CommunityAxis eixo,
        CommunityVisibility visibilidade,
        string? password,
        double? lat,
        double? lng,
        string? bairro,
        string? cidade,
        string? estado,
        Guid criadorId,
        string criadorNome,
        string? criadorAvatarUrl)
    {
        ValidateInvariants(nome, tipo, visibilidade, password, criadorId);

        return new CommunityGroup
        {
            Id = Guid.NewGuid(),
            Name = nome.Trim(),
            Description = descricao?.Trim() ?? string.Empty,
            Type = tipo,
            Axis = eixo,
            Visibility = visibilidade,
            PasswordHash = null,
            Lat = lat,
            Lng = lng,
            Neighborhood = bairro,
            City = cidade,
            State = estado,
            CreatorId = criadorId,
            CreatorName = string.IsNullOrWhiteSpace(criadorNome) ? "Usuário" : criadorNome,
            CreatorAvatarUrl = criadorAvatarUrl,
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

    public void Archive()
    {
        if (Status == CommunityStatus.Archived)
        {
            throw new DomainException("Comunidade já está arquivada.");
        }

        Status = CommunityStatus.Archived;
    }

    private static void ValidateInvariants(
        string nome,
        CommunityType tipo,
        CommunityVisibility visibilidade,
        string? password,
        Guid criadorId)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DomainException("Nome da comunidade é obrigatório.");
        }

        // Default (uma por cidade) é sempre Open.
        if (tipo == CommunityType.Default && visibilidade != CommunityVisibility.Open)
        {
            throw new DomainException("Comunidades Default são sempre Open.");
        }

        if (visibilidade == CommunityVisibility.Private && string.IsNullOrWhiteSpace(password))
        {
            throw new DomainException("Comunidades privadas exigem senha.");
        }

        if (criadorId == Guid.Empty)
        {
            throw new DomainException("Criador é obrigatório.");
        }
    }
}
