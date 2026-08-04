using Revoa.Community.Domain.Aggregates.CommunityAggregate;

namespace Revoa.Community.Application.DTOs;

// Item do feed / detalhe de comunidade. MembrosCount vem de agregação (anti-N+1 via batch count).
public sealed record CommunityDto(
    Guid Id,
    string Nome,
    string Descricao,
    CommunityTipo Tipo,
    CommunityEixo Eixo,
    CommunityVisibilidade Visibilidade,
    double? Lat,
    double? Lng,
    string? Bairro,
    string? Cidade,
    string? Estado,
    Guid CriadorId,
    string CriadorNome,
    string? CriadorAvatarUrl,
    int MembrosCount);

public static class CommunityDtoMapper
{
    public static CommunityDto From(CommunityGroup c, int membrosCount) => new(
        c.Id,
        c.Nome,
        c.Descricao,
        c.Tipo,
        c.Eixo,
        c.Visibilidade,
        c.Lat,
        c.Lng,
        c.Bairro,
        c.Cidade,
        c.Estado,
        c.CriadorId,
        c.CriadorNome,
        c.CriadorAvatarUrl,
        membrosCount);
}
