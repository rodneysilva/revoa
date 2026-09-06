using Revoa.Community.Domain.Aggregates.MembershipAggregate;
using Revoa.Community.Domain.Repositories;
using Revoa.IntegrationContracts.Communities;

namespace Revoa.Community.Infrastructure.Services;

// Adapter da porta IMembershipStatusChecker: traduz o aggregate Membership no
// veredito booleano que o Catalog consome (mesmo papel do WalletAddressReader).
public class MembershipStatusChecker : IMembershipStatusChecker
{
    private readonly IMembershipRepository _memberships;

    public MembershipStatusChecker(IMembershipRepository memberships)
    {
        _memberships = memberships;
    }

    public async Task<bool> IsActiveMemberAsync(
        Guid userId, Guid communityId, CancellationToken ct = default)
    {
        var membership = await _memberships.GetByUserAndCommunityAsync(userId, communityId, ct);
        return membership?.Status == MembershipStatus.Active;
    }
}
