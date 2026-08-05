using MediatR;
using Revoa.Abstractions;
using Revoa.Reputation.Application.DTOs;
using Revoa.Reputation.Domain.Repositories;

namespace Revoa.Reputation.Application.Queries;

// Consulta as avaliações recebidas por um usuário (leitura pública, anônima — perfil/ListingDetail).
public sealed record GetUserReviewsQuery(Guid RevieweeId, int Limit) : IRequest<Result<IReadOnlyList<ReviewDto>>>;

public class GetUserReviewsQueryHandler : IRequestHandler<GetUserReviewsQuery, Result<IReadOnlyList<ReviewDto>>>
{
    private readonly IReviewRepository _reviews;

    public GetUserReviewsQueryHandler(IReviewRepository reviews)
    {
        _reviews = reviews;
    }

    public async Task<Result<IReadOnlyList<ReviewDto>>> Handle(GetUserReviewsQuery request, CancellationToken ct)
    {
        if (request.RevieweeId == Guid.Empty)
        {
            return Result<IReadOnlyList<ReviewDto>>.Fail("Usuário é obrigatório.");
        }

        var cap = request.Limit > 0 && request.Limit <= 100 ? request.Limit : 20;
        var reviews = await _reviews.GetByRevieweeAsync(request.RevieweeId, cap, ct);
        var dtos = reviews.Select(ReviewDtoMapper.From).ToList();
        return Result<IReadOnlyList<ReviewDto>>.Ok(dtos);
    }
}
