using MediatR;
using Revoa.Abstractions;
using Revoa.Notifications.Application.DTOs;
using Revoa.Notifications.Domain.Repositories;

namespace Revoa.Notifications.Application.Queries;

// Lista notificações do usuário (50 por página). UnreadOnly filtra só não-lidas.
public sealed record GetNotificationsQuery(Guid UserId, bool UnreadOnly, int Page)
    : IRequest<Result<IReadOnlyList<NotificationDto>>>;

public class GetNotificationsQueryHandler
    : IRequestHandler<GetNotificationsQuery, Result<IReadOnlyList<NotificationDto>>>
{
    private const int PageSize = 50;

    private readonly INotificationRepository _notifications;

    public GetNotificationsQueryHandler(INotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task<Result<IReadOnlyList<NotificationDto>>> Handle(GetNotificationsQuery request, CancellationToken ct)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var skip = (page - 1) * PageSize;

        var items = await _notifications.GetByUserAsync(request.UserId, request.UnreadOnly, PageSize, skip, ct);

        IReadOnlyList<NotificationDto> result = items
            .Select(NotificationDtoMapper.From)
            .ToList();

        return Result<IReadOnlyList<NotificationDto>>.Ok(result);
    }
}
