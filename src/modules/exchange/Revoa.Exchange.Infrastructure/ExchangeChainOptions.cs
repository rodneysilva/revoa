namespace Revoa.Exchange.Infrastructure;

// Liga-se à mesma seção "Chain" do appsettings (compartilha RpcUrl/ChainId/FaucetPrivateKey),
// mas lê os contratos que o Exchange orquestra (EscrowVault + ServiceVoucher + RVM + ProductNFT).
// Mantém isolamento do módulo (não referencia Revoa.Token/Catalog.Infrastructure).
public class ExchangeChainOptions
{
    public const string SectionName = "Chain";

    public string RpcUrl { get; set; } = "http://127.0.0.1:8545";

    public long ChainId { get; set; } = 31337;

    public string FaucetPrivateKey { get; set; } = string.Empty;

    // Chave dedicada do árbitro (resolve de disputas assina claimArbitrator). Opcional: vazia →
    // fallback para a faucet (comportamento dev/anvil). Em chain real, usar chave separada
    // (Chain__ArbitratorPrivateKey) — quem resolve disputas não deveria controlar o faucet.
    public string ArbitratorPrivateKey { get; set; } = string.Empty;

    public ExchangeChainContracts Contracts { get; set; } = new();
}

public class ExchangeChainContracts
{
    public string RVM { get; set; } = string.Empty;

    public string EscrowVault { get; set; } = string.Empty;

    public string ProductNFT { get; set; } = string.Empty;

    public string ServiceVoucher { get; set; } = string.Empty;
}
