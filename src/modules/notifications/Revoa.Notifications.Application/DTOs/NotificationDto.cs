using Revoa.Notifications.Domain.Aggregates.NotificationAggregate;

namespace Revoa.Notifications.Application.DTOs;

public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Body,
    string? Payload,
    bool Read,
    DateTime? ReadAt,
    DateTime CreatedAt);

public static class NotificationDtoMapper
{
    public static NotificationDto From(Notification n) => new(
        n.Id,
        n.Type.ToString(),
        n.Title,
        n.Body,
        n.Payload,
        n.Read,
        n.ReadAt,
        n.CreatedAt);
}
