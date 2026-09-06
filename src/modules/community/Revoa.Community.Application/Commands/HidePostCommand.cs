using MediatR;
using Revoa.Abstractions;
using Revoa.Community.Domain.Aggregates.MembershipAggregate;
using Revoa.Community.Domain.Aggregates.PostAggregate;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Application.Commands;

// Oculta post em cascata (post + descendentes via prefix do Path). Exige Moderador ou Criador.
public sealed record HidePostCommand(Guid PostId, string OcultadoPor, Guid ModeradorId) : IRequest<Result>;

public class HidePostCommandHandler : IRequestHandler<HidePostCommand, Result>
{
    private readonly IPostRepository _posts;
    private readonly IMembershipRepository _memberships;

    public HidePostCommandHandler(IPostRepository posts, IMembershipRepository memberships)
    {
        _posts = posts;
        _memberships = memberships;
    }

    public async Task<Result> Handle(HidePostCommand request, CancellationToken ct)
    {
        var post = await _posts.GetByIdAsync(request.PostId, ct);
        if (post is null)
        {
            return Result.Fail("Post não encontrado.");
        }

        // Gate: moderador daquela comunidade (Moderador ou Criador).
        var membership = await _memberships.GetByUsuarioEComunidadeAsync(request.ModeradorId, post.CommunityId, ct);
        if (membership is null
            || membership.Status != MembershipStatus.Ativa
            || membership.Role is not (MembershipRole.Moderator or MembershipRole.Creator))
        {
            return Result.Fail("Apenas moderadores ou o criador podem ocultar posts.");
        }

        try
        {
            post.Ocultar(request.OcultadoPor);
        }
        catch (DomainException ex)
        {
            return Result.Fail(ex.Message);
        }

        await _posts.UpdateAsync(post, ct);

        // Oculta descendentes cujo Path inicia com o Path do post (prefix regex).
        await _posts.HideCascadeAsync(post.Path, request.OcultadoPor, ct);

        return Result.Ok();
    }
}
