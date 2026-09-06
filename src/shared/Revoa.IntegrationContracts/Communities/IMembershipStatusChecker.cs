namespace Revoa.IntegrationContracts.Communities;

// Porta (anti-corruption layer): Catalog valida — sem acessar o módulo Community —
// que o usuário tem vínculo Active antes de escopar um anúncio a uma comunidade.
// O adapter vive em Revoa.Community.Infrastructure, que lê o aggregate Membership
// e devolve só o veredito. Espelha IWalletAddressReader (Accounts).
//
// Catalog não referencia Community; consome só este port.
public interface IMembershipStatusChecker
{
    // True se o usuário tem vínculo Active na comunidade.
    Task<bool> IsActiveMemberAsync(Guid userId, Guid communityId, CancellationToken ct = default);
}
