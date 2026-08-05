using ReputationAggregate = Revoa.Reputation.Domain.Aggregates.ReputationAggregate;

namespace Revoa.Reputation.Application.DTOs;

// Leitura pública do score de reputação de um usuário (GET /api/users/{id}/reputation).
public sealed record ReputationDto(
    Guid UserId,
    long Points,
    string Level,
    long HelpPoints,
    int DonationsCount,
    int VolunteerCount,
    int ReviewsCount,
    double AvgRating);

public static class ReputationDtoMapper
{
    public static ReputationDto From(ReputationAggregate.Reputation rep) => new(
        rep.UserId,
        rep.Points,
        rep.Level,
        rep.HelpPoints,
        rep.DonationsCount,
        rep.VolunteerCount,
        rep.ReviewsCount,
        rep.AvgRating);
}
