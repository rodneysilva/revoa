using MediatR;
using Revoa.Notifications.Domain.Repositories;

namespace Revoa.Notifications.Application.Queries;

// Contagem de não-lidas (badge do sino). Desvio: retorna int direto — Result<T> exige T : class
// (int é struct). Read puro sem modo de falha de negócio; em erro de banco a exceção sobe.
public sealed record GetUnreadCountQuery(Guid UserId) : IRequest<int>;

public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, int>
{
    private readonly INotificationRepository _notifications;

    public GetUnreadCountQueryHandler(INotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task<int> Handle(GetUnreadCountQuery request, CancellationToken ct)
    {
        return await _notifications.GetUnreadCountAsync(request.UserId, ct);
    }
}
