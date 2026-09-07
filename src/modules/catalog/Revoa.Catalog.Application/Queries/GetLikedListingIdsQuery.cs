using MediatR;
using Revoa.Catalog.Domain.Repositories;

namespace Revoa.Catalog.Application.Queries;

// Ids dos anúncios curtidos pelo usuário (bootstrap do botão curtir no FE —
// uma chamada, sem N+1 por card). Desvio do Result<T>: lista não tem modo de
// falha de negócio (paridade com GetSavedListingIdsQuery).
public sealed record GetLikedListingIdsQuery(Guid UserId)
    : IRequest<IReadOnlyList<Guid>>;

public class GetLikedListingIdsQueryHandler
    : IRequestHandler<GetLikedListingIdsQuery, IReadOnlyList<Guid>>
{
    private readonly IListingLikeRepository _likes;

    public GetLikedListingIdsQueryHandler(IListingLikeRepository likes)
    {
        _likes = likes;
    }

    public async Task<IReadOnlyList<Guid>> Handle(
        GetLikedListingIdsQuery request, CancellationToken ct)
    {
        return await _likes.GetAllLikedIdsAsync(request.UserId, ct);
    }
}
