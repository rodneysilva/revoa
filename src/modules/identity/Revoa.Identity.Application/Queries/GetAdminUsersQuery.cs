using MediatR;
using Revoa.Abstractions;
using Revoa.Identity.Domain.Aggregates.UserAggregate;
using Revoa.Identity.Domain.Repositories;

namespace Revoa.Identity.Application.Queries;

// Lista de usuários para o painel admin (todos os status, mais recentes primeiro).
public sealed record GetAdminUsersQuery : IRequest<Result<IReadOnlyList<AdminUserDto>>>;

public sealed record AdminUserDto(
    Guid Id,
    string Name,
    string Email,
    string Phone,
    string Status,
    bool EmailVerified,
    bool PhoneVerified,
    DateTime? MemberSince);

public class GetAdminUsersQueryHandler
    : IRequestHandler<GetAdminUsersQuery, Result<IReadOnlyList<AdminUserDto>>>
{
    private readonly IUserRepository _users;

    public GetAdminUsersQueryHandler(IUserRepository users)
    {
        _users = users;
    }

    public async Task<Result<IReadOnlyList<AdminUserDto>>> Handle(
        GetAdminUsersQuery request, CancellationToken ct)
    {
        var users = await _users.GetAllAsync(limit: 500, ct);

        IReadOnlyList<AdminUserDto> result = users
            .Select(u => new AdminUserDto(
                u.Id,
                u.Name,
                u.Email,
                u.Phone,
                u.Status.ToString(),
                u.EmailVerified,
                u.PhoneVerified,
                u.CreatedAt == default ? null : u.CreatedAt))
            .ToList();

        return Result<IReadOnlyList<AdminUserDto>>.Ok(result);
    }
}
