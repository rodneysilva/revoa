using MediatR;
using Revoa.Reputation.Application.DTOs;
using Revoa.Reputation.Domain.Repositories;

namespace Revoa.Reputation.Application.Queries;

// Consulta o score de reputação de um usuário (leitura pública, anônima). 404 se inexistente.
public sealed record GetReputationQuery(Guid UserId) : IRequest<ReputationDto?>;

public class GetReputationQueryHandler : IRequestHandler<GetReputationQuery, ReputationDto?>
{
    private readonly IReputationRepository _reputation;

    public GetReputationQueryHandler(IReputationRepository reputation)
    {
        _reputation = reputation;
    }

    public async Task<ReputationDto?> Handle(GetReputationQuery request, CancellationToken ct)
    {
        var rep = await _reputation.GetByUserIdAsync(request.UserId, ct);
        return rep is null ? null : ReputationDtoMapper.From(rep);
    }
}
