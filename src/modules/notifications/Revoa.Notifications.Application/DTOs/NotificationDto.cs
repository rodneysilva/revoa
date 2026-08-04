using Revoa.Notifications.Domain.Aggregates.NotificationAggregate;

namespace Revoa.Notifications.Application.DTOs;

public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Titulo,
    string Corpo,
    string? Payload,
    bool Lida,
    DateTime? ReadAt,
    DateTime CreatedAt);

public static class NotificationDtoMapper
{
    public static NotificationDto From(Notification n) => new(
        n.Id,
        n.Type.ToString(),
        n.Titulo,
        n.Corpo,
        n.Payload,
        n.Lida,
        n.ReadAt,
        n.CreatedAt);
}
