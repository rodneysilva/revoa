using MediatR;
using Revoa.Abstractions;
using Revoa.Identity.Domain.Repositories;

namespace Revoa.Identity.Application.Commands;

public sealed record VerifyEmailCommand(Guid UserId, string Token) : IRequest<Result>;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result>
{
    private readonly IUserRepository _users;

    public VerifyEmailCommandHandler(IUserRepository users)
    {
        _users = users;
    }

    public async Task<Result> Handle(VerifyEmailCommand request, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(request.UserId, ct);
        if (user is null)
        {
            // Mensagem neutra para não revelar existência do UserId.
            return Result.Fail("Verificação inválida ou expirada.");
        }

        if (!user.VerifyEmail(request.Token ?? string.Empty))
        {
            return Result.Fail("Verificação inválida ou expirada.");
        }

        await _users.UpdateAsync(user, ct);
        return Result.Ok();
    }
}
