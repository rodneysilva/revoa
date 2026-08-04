using Revoa.Abstractions;

namespace Revoa.Community.Domain.Aggregates.CommunityAggregate;

public enum CommunityTipo
{
    Default,
    User
}

public enum CommunityEixo
{
    Geo,
    Interesse,
    Causa
}

public enum CommunityVisibilidade
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
    public string Nome { get; private set; } = string.Empty;
    public string Descricao { get; private set; } = string.Empty;
    public CommunityTipo Tipo { get; private set; }
    public CommunityEixo Eixo { get; private set; }
    public CommunityVisibilidade Visibilidade { get; private set; }

    // Só para Private.
    public string? PasswordHash { get; private set; }

    public double? Lat { get; private set; }
    public double? Lng { get; private set; }
    public string? Bairro { get; private set; }
    public string? Cidade { get; private set; }
    public string? Estado { get; private set; }

    public Guid CriadorId { get; private set; }
    public string CriadorNome { get; private set; } = string.Empty;
    public string? CriadorAvatarUrl { get; private set; }

    public CommunityStatus Status { get; private set; }

    private CommunityGroup() { }

    public static CommunityGroup Create(
        string nome,
        string descricao,
        CommunityTipo tipo,
        CommunityEixo eixo,
        CommunityVisibilidade visibilidade,
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
            Nome = nome.Trim(),
            Descricao = descricao?.Trim() ?? string.Empty,
            Tipo = tipo,
            Eixo = eixo,
            Visibilidade = visibilidade,
            PasswordHash = null,
            Lat = lat,
            Lng = lng,
            Bairro = bairro,
            Cidade = cidade,
            Estado = estado,
            CriadorId = criadorId,
            CriadorNome = string.IsNullOrWhiteSpace(criadorNome) ? "Usuário" : criadorNome,
            CriadorAvatarUrl = criadorAvatarUrl,
            Status = CommunityStatus.Active,
            Version = 1
        };
    }

    // Seta o hash da senha (apenas Private). Usado na criação (handler) e na rotação de senha.
    public void SetPasswordHash(string hash)
    {
        if (Visibilidade != CommunityVisibilidade.Private)
        {
            throw new DomainException("Apenas comunidades privadas possuem senha.");
        }

        PasswordHash = hash;
        IncrementVersion();
    }

    public void Archive()
    {
        if (Status == CommunityStatus.Archived)
        {
            throw new DomainException("Comunidade já está arquivada.");
        }

        Status = CommunityStatus.Archived;
        IncrementVersion();
    }

    private static void ValidateInvariants(
        string nome,
        CommunityTipo tipo,
        CommunityVisibilidade visibilidade,
        string? password,
        Guid criadorId)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DomainException("Nome da comunidade é obrigatório.");
        }

        // Default (uma por cidade) é sempre Open.
        if (tipo == CommunityTipo.Default && visibilidade != CommunityVisibilidade.Open)
        {
            throw new DomainException("Comunidades Default são sempre Open.");
        }

        if (visibilidade == CommunityVisibilidade.Private && string.IsNullOrWhiteSpace(password))
        {
            throw new DomainException("Comunidades privadas exigem senha.");
        }

        if (criadorId == Guid.Empty)
        {
            throw new DomainException("Criador é obrigatório.");
        }
    }
}
