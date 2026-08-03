using MediatR;
using Revoa.Abstractions;
using Revoa.Identity.Domain.Repositories;

namespace Revoa.Identity.Application.Commands;

public sealed record VerifyPhoneCommand(Guid UserId, string Code) : IRequest<Result>;

public class VerifyPhoneCommandHandler : IRequestHandler<VerifyPhoneCommand, Result>
{
    private readonly IUserRepository _users;

    public VerifyPhoneCommandHandler(IUserRepository users)
    {
        _users = users;
    }

    public async Task<Result> Handle(VerifyPhoneCommand request, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(request.UserId, ct);
        if (user is null)
        {
            return Result.Fail("Usuário não encontrado.");
        }

        user.MarkPhoneVerified();
        await _users.UpdateAsync(user, ct);
        return Result.Ok();
    }
}
