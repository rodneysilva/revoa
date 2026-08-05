using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Parâmetros de sistema runtime (off-chain, admin, UF-30). 8 parâmetros conhecidos; PUT atualiza e
// GET confirma; chave desconhecida → 400 (whitelist de segurança).
public class AdminParamsTests : IntegrationTestBase
{
    public AdminParamsTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Get_returns_eight_parameters()
    {
        var adminClient = AuthedClient(await CreateAdminAsync());

        var resp = await adminClient.GetAsync("/api/admin/parameters");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var arr = await resp.Content.ReadFromJsonAsync<JsonArray>();
        arr.Should().NotBeNull();
        arr!.Count.Should().Be(8);
    }

    [Fact]
    public async Task Put_updates_bonus_rvm_and_get_confirms()
    {
        var adminClient = AuthedClient(await CreateAdminAsync());

        var put = await adminClient.PutAsync(
            "/api/admin/parameters/DonationReward.BonusRvm",
            JsonBody(new { Value = 7 }));
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        // GET confirma o novo valor.
        var get = await adminClient.GetAsync("/api/admin/parameters");
        var arr = await get.Content.ReadFromJsonAsync<JsonArray>();
        var bonus = arr!.Single(p => p!["Key"]!.GetValue<string>() == "DonationReward.BonusRvm");
        bonus!["Value"]!.GetValue<int>().Should().Be(7);
    }

    [Fact]
    public async Task Put_unknown_key_returns_400()
    {
        var adminClient = AuthedClient(await CreateAdminAsync());

        var put = await adminClient.PutAsync(
            "/api/admin/parameters/Chave.Inexistente.E2E",
            JsonBody(new { Value = 1 }));
        put.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
