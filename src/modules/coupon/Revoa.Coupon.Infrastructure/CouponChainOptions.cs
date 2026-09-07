namespace Revoa.Coupon.Infrastructure;

// Liga-se à mesma seção "Chain" do appsettings (compartilha RpcUrl/ChainId/FaucetPrivateKey),
// mas lê apenas o contrato que o módulo Coupon orquestra (CouponRedeemer). Mantém isolamento do
// módulo (não referencia Revoa.Token/Exchange.Infrastructure).
public class CouponChainOptions
{
    public const string SectionName = "Chain";

    public string RpcUrl { get; set; } = "http://127.0.0.1:8545";

    public long ChainId { get; set; } = 31337;

    public string FaucetPrivateKey { get; set; } = string.Empty;

    // Mesma flag/env do módulo Token (Chain__FundWalletGas*): em dev, a faucet repõe
    // gás de carteiras sem ETH antes de o usuário assinar o redeem (legados do seed
    // nunca passaram pelo faucet). Em produção fica false (gas vem do Paymaster/AA).
    public bool FundWalletGasOnCreate { get; set; } = false;

    public decimal FundWalletGasEther { get; set; } = 1m;

    public CouponChainContracts Contracts { get; set; } = new();
}

public class CouponChainContracts
{
    public string CouponRedeemer { get; set; } = string.Empty;
}
