using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Revoa.IntegrationTests.Harness;

// Sonda a chain de dev (anvil em 127.0.0.1:8545). Os fluxos on-chain (troca/faucet/cupom/doação)
// só rodam com anvil + contratos deployados (bootstrap-dev.ps1). Se indisponível, os testes
// on-chain chamam Skip.IfNot(ChainProbe.IsAvailable(...)) e pulam graceful.
public static class ChainProbe
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(3) };

    private static readonly string ProbeBody =
        "{\"jsonrpc\":\"2.0\",\"method\":\"eth_blockNumber\",\"params\":[],\"id\":1}";

    // Tenta eth_blockNumber no RpcUrl da config; qualquer falha = chain indisponível.
    public static bool IsAvailable(IConfiguration config)
    {
        var url = config["Chain:RpcUrl"];
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        try
        {
            using var content = new StringContent(ProbeBody, Encoding.UTF8, "application/json");
            using var resp = Http.PostAsync(url, content).GetAwaiter().GetResult();
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
