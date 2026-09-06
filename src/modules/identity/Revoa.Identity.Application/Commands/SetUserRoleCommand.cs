using MediatR;
using Revoa.Abstractions;
using Revoa.Identity.Domain.Aggregates.UserAggregate;
using Revoa.Identity.Domain.Repositories;

namespace Revoa.Identity.Application.Commands;

// Admin altera a role de um usuário (User/Mod/Admin/Arbitrator). Único caminho de escrita de
// UserRole — alimenta a claim "role" do JWT (policies "Arbitrator" etc.). updatedBy = e-mail do
// admin (claim do token), apenas para log/auditoria.
public sealed record SetUserRoleCommand(Guid UserId, UserRole Role, string UpdatedBy) : IRequest<Result>;

public class SetUserRoleCommandHandler : IRequestHandler<SetUserRoleCommand, Result>
{
    private readonly IUserRepository _users;

    public SetUserRoleCommandHandler(IUserRepository users)
    {
        _users = users;
    }

    public async Task<Result> Handle(SetUserRoleCommand request, CancellationToken ct)
    {
        if (!Enum.IsDefined(request.Role))
        {
            return Result.Fail("Role inválida.");
        }

        var user = await _users.GetByIdAsync(request.UserId, ct);
        if (user is null)
        {
            return Result.Fail("Usuário não encontrado.");
        }

        user.SetRole(request.Role);
        await _users.UpdateAsync(user, ct);
        return Result.Ok();
    }
}
