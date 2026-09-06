using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Saldo da carteira (off-chain, sempre verde). O balanceOf em si toca a chain — sem anvil
// a suite espera degradação (Rvm = null), nunca erro: o chip de saldo não pode derrubar a UI.
public class WalletTests : IntegrationTestBase
{
    public WalletTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Balance_anonymous_returns_401()
    {
        var resp = await Http.GetAsync("/api/wallet/balance");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Balance_for_registered_user_returns_wallet_and_degraded_rvm()
    {
        // Registro dispara UserRegisteredEvent → Account (EOA) criada in-process.
        var user = await CreateUserAsync();
        var client = AuthedClient(user);

        var resp = await client.GetAsync("/api/wallet/balance");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json.Should().NotBeNull();
        var address = json!["WalletAddress"]?.GetValue<string>();
        address.Should().NotBeNullOrWhiteSpace("o registro cria a EOA via evento in-process");
        address!.Should().StartWith("0x");

        // Sem chain rodando (OffChain): degrada para null em vez de 500.
        var rvm = json["Rvm"];
        if (rvm is not null)
        {
            rvm.GetValue<decimal>().Should().BeGreaterThanOrEqualTo(0m);
        }
    }
}
