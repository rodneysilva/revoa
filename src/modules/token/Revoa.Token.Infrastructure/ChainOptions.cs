namespace Revoa.Token.Infrastructure;

public class ChainOptions
{
    public const string SectionName = "Chain";

    public string RpcUrl { get; set; } = "http://127.0.0.1:8545";

    public long ChainId { get; set; } = 31337;

    public string FaucetPrivateKey { get; set; } = string.Empty;

    public ChainContracts Contracts { get; set; } = new();
}

public class ChainContracts
{
    public string RVM { get; set; } = string.Empty;
}
