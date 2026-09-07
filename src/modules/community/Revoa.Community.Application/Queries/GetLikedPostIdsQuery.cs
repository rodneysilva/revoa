using MediatR;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Application.Queries;

// Ids de posts curtidos pelo usuário (bootstrap do botão ❤ no FE). Desvia do
// Result<T> — leitura sem falha de negócio (padrão GetSavedListingIdsQuery).
public sealed record GetLikedPostIdsQuery(Guid UserId) : IRequest<IReadOnlyList<Guid>>;

public class GetLikedPostIdsQueryHandler
    : IRequestHandler<GetLikedPostIdsQuery, IReadOnlyList<Guid>>
{
    private readonly IPostLikeRepository _likes;

    public GetLikedPostIdsQueryHandler(IPostLikeRepository likes)
    {
        _likes = likes;
    }

    public Task<IReadOnlyList<Guid>> Handle(GetLikedPostIdsQuery request, CancellationToken ct)
    {
        return _likes.GetAllLikedIdsAsync(request.UserId, ct);
    }
}
