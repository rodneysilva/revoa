using MediatR;
using Revoa.Abstractions;
using Revoa.Identity.Domain.Repositories;

namespace Revoa.Identity.Application.Commands;

// Banir/reabilitar usuário (painel admin). Banned bloqueia login e ações; Unban
// devolve Active. O próprio admin não pode se banir (trava de pé em pé).
public sealed record SetUserBannedCommand(Guid AdminId, Guid UserId, bool Banned)
    : IRequest<Result>;

public class SetUserBannedCommandHandler
    : IRequestHandler<SetUserBannedCommand, Result>
{
    private readonly IUserRepository _users;

    public SetUserBannedCommandHandler(IUserRepository users)
    {
        _users = users;
    }

    public async Task<Result> Handle(SetUserBannedCommand request, CancellationToken ct)
    {
        if (request.UserId == request.AdminId)
        {
            return Result.Fail("Você não pode banir a si mesmo.");
        }

        var user = await _users.GetByIdAsync(request.UserId, ct);
        if (user is null)
        {
            return Result.Fail("Usuário não encontrado.");
        }

        if (request.Banned)
        {
            user.Ban();
        }
        else
        {
            try
            {
                user.Unban();
            }
            catch (DomainException)
            {
                return Result.Fail("Usuário não está banido.");
            }
        }

        await _users.UpdateAsync(user, ct);
        return Result.Ok();
    }
}
