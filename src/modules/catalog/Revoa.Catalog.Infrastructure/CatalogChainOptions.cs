namespace Revoa.Catalog.Infrastructure;

// Liga-se à mesma seção "Chain" do appsettings (compartilha RpcUrl/ChainId/FaucetPrivateKey),
// mas lê só os contratos que o Catalog usa (ProductNFT + EscrowVault). Mantém isolamento do módulo
// (não referencia Revoa.Token.Infrastructure).
public class CatalogChainOptions
{
    public const string SectionName = "Chain";

    public string RpcUrl { get; set; } = "http://127.0.0.1:8545";

    public long ChainId { get; set; } = 31337;

    public string FaucetPrivateKey { get; set; } = string.Empty;

    public CatalogChainContracts Contracts { get; set; } = new();
}

public class CatalogChainContracts
{
    public string ProductNFT { get; set; } = string.Empty;

    public string EscrowVault { get; set; } = string.Empty;
}
