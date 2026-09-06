using Revoa.Community.Domain.Aggregates.CommunityAggregate;

namespace Revoa.Community.Application.DTOs;

// Item do feed / detalhe de comunidade. MembersCount vem de agregação (anti-N+1 via batch count).
public sealed record CommunityDto(
    Guid Id,
    string Name,
    string Description,
    CommunityType Type,
    CommunityAxis Axis,
    CommunityVisibility Visibility,
    double? Lat,
    double? Lng,
    string? Neighborhood,
    string? City,
    string? State,
    Guid CreatorId,
    string CreatorName,
    string? CreatorAvatarUrl,
    int MembersCount);

public static class CommunityDtoMapper
{
    public static CommunityDto From(CommunityGroup c, int membrosCount) => new(
        c.Id,
        c.Name,
        c.Description,
        c.Type,
        c.Axis,
        c.Visibility,
        c.Lat,
        c.Lng,
        c.Neighborhood,
        c.City,
        c.State,
        c.CreatorId,
        c.CreatorName,
        c.CreatorAvatarUrl,
        membrosCount);
}
