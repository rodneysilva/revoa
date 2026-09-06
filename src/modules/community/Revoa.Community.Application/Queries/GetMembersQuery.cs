using MediatR;
using Revoa.Abstractions;
using Revoa.Community.Application.DTOs;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Application.Queries;

// Lista de membros de uma comunidade (anônimo vê).
public sealed record GetMembersQuery(Guid CommunityId) : IRequest<Result<IReadOnlyList<MembershipDto>>>;

public class GetMembersQueryHandler : IRequestHandler<GetMembersQuery, Result<IReadOnlyList<MembershipDto>>>
{
    private readonly IMembershipRepository _memberships;

    public GetMembersQueryHandler(IMembershipRepository memberships)
    {
        _memberships = memberships;
    }

    public async Task<Result<IReadOnlyList<MembershipDto>>> Handle(GetMembersQuery request, CancellationToken ct)
    {
        var members = await _memberships.ListByCommunityAsync(request.CommunityId, ct);
        IReadOnlyList<MembershipDto> result = members.Select(MembershipDtoMapper.From).ToList();
        return Result<IReadOnlyList<MembershipDto>>.Ok(result);
    }
}
