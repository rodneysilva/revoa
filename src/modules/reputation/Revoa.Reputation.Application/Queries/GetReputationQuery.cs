using MediatR;
using Revoa.Reputation.Application.DTOs;
using Revoa.Reputation.Domain.Repositories;
using ReputationAggregate = Revoa.Reputation.Domain.Aggregates.ReputationAggregate;

namespace Revoa.Reputation.Application.Queries;

// Consulta o score de reputação de um usuário (leitura pública, anônima).
// Sem documento ainda => score zerado ("Iniciante"), NÃO 404: ausência de
// reputação é estado válido (usuário recém-chegado), e 404 fazia o /profile
// logar erro de console no primeiro acesso de qualquer conta nova.
public sealed record GetReputationQuery(Guid UserId) : IRequest<ReputationDto>;

public class GetReputationQueryHandler : IRequestHandler<GetReputationQuery, ReputationDto>
{
    private readonly IReputationRepository _reputation;

    public GetReputationQueryHandler(IReputationRepository reputation)
    {
        _reputation = reputation;
    }

    public async Task<ReputationDto> Handle(GetReputationQuery request, CancellationToken ct)
    {
        var rep = await _reputation.GetByUserIdAsync(request.UserId, ct)
                  ?? ReputationAggregate.Reputation.Create(request.UserId);
        return ReputationDtoMapper.From(rep);
    }
}
