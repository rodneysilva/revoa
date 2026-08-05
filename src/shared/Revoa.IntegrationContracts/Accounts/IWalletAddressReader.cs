namespace Revoa.IntegrationContracts.Accounts;

// Porta (anti-corruption layer): lista todos os endereços de carteira para o Demurrage queimar,
// SEM acessar a coleção Accounts diretamente (isolamento de módulos: "nunca importar repository de
// outro módulo"). O adapter vive em Revoa.Account.Infrastructure, que lê o aggregate UserAccount e
// devolve só o necessário (UserId + Address). Espelha IUserWalletProvider.
//
// Demurrage não referencia Account; consome só este port.
public sealed record WalletAddressEntry(Guid UserId, string Address);

public interface IWalletAddressReader
{
    // Todas as carteiras conhecidas (MVP dev: EOAs em UserAccount). Demurrage itera para ler
    // saldos e queimar acima do piso.
    Task<IReadOnlyList<WalletAddressEntry>> GetAllAsync(CancellationToken ct = default);
}
