namespace Revoa.Token.Infrastructure;

public class ChainOptions
{
    public const string SectionName = "Chain";

    public string RpcUrl { get; set; } = "http://127.0.0.1:8545";

    public long ChainId { get; set; } = 31337;

    public string FaucetPrivateKey { get; set; } = string.Empty;

    // DEV/anvil: financia a nova carteira com ETH (gas) ao criar. Em produção o gas é do
    // Paymaster/AA relayer (não da faucet). Default false; true em Development.
    public bool FundWalletGasOnCreate { get; set; } = false;

    // Quantidade de ETH (em ether) enviada a cada nova carteira em dev.
    public decimal FundWalletGasEther { get; set; } = 1m;

    public ChainContracts Contracts { get; set; } = new();
}

public class ChainContracts
{
    public string RVM { get; set; } = string.Empty;
}
