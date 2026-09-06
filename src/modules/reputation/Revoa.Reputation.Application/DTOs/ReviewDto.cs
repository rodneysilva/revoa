using Revoa.Reputation.Domain.Aggregates.ReviewAggregate;

namespace Revoa.Reputation.Application.DTOs;

// Leitura de uma avaliação recebida (GET /api/users/{id}/reviews).
public sealed record ReviewDto(
    Guid Id,
    Guid TradeId,
    Guid ReviewerId,
    string ReviewerName,
    int Rating,
    string? Comment,
    DateTime CreatedAt);

public static class ReviewDtoMapper
{
    public static ReviewDto From(Review r) => new(
        r.Id,
        r.TradeId,
        r.ReviewerId,
        r.ReviewerName,
        r.Rating,
        r.Comment,
        r.CreatedAt);
}
