using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Demurrage (off-chain, sempre verde). Preview/run de verdade tocam a chain (balanceOf/burn);
// aqui cobrimos os gates Admin (401/403), o histórico (Mongo puro) e duas vias off-chain:
// - Preview: resiliente por carteira (falha de saldo é pulada, nunca derruba o 200) e com DB
//   zerado não há carteiras acima do piso → shape zerado independente da chain.
// - Run: com o parâmetro runtime Demurrage.Enabled=false (via /api/admin/parameters) o handler
//   falha ANTES de qualquer chamada on-chain — valida a integração parâmetros↔demurrage.
public class DemurrageTests : IntegrationTestBase
{
    public DemurrageTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Preview_requires_admin()
    {
        var anonymous = await Http.PostAsync("/api/demurrage/preview", content: null);
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var user = await CreateUserAsync();
        var byUser = await AuthedClient(user).PostAsync("/api/demurrage/preview", content: null);
        byUser.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Preview_returns_zero_burn_shape_for_admin()
    {
        var admin = await CreateAdminAsync();

        var resp = await AuthedClient(admin).PostAsync("/api/demurrage/preview", content: null);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["RateBps"]!.GetValue<int>().Should().Be(50); // default Demurrage:MonthlyRateBps
        json["FloorRvm"]!.GetValue<long>().Should().Be(100); // default Demurrage:FloorRvm
        json["AccountsAffected"]!.GetValue<int>().Should().Be(0);
        json["Skipped"]!.GetValue<int>().Should().BeGreaterThanOrEqualTo(0);
        json["TotalBurnedRaw"]!.GetValue<string>().Should().Be("0");
        json["TotalBurnedRvm"]!.GetValue<decimal>().Should().Be(0);
    }

    [Fact]
    public async Task Run_requires_admin()
    {
        var anonymous = await Http.PostAsync("/api/demurrage/run", content: null);
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var user = await CreateUserAsync();
        var byUser = await AuthedClient(user).PostAsync(
            "/api/demurrage/run", JsonBody(new { ExecutedBy = (string?)null }));
        byUser.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Run_with_module_disabled_returns_400_without_touching_chain()
    {
        var admin = await CreateAdminAsync();
        var adminClient = AuthedClient(admin);

        // Parâmetro runtime (UF-30): desativa o módulo — passa a valer na hora, sem restart.
        var put = await adminClient.PutAsync(
            "/api/admin/parameters/Demurrage.Enabled", JsonBody(new { Value = false }));
        put.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var resp = await adminClient.PostAsync(
            "/api/demurrage/run", JsonBody(new { ExecutedBy = (string?)null }));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["Error"]!.GetValue<string>().Should().Be("Demurrage desativado.");
    }

    [Fact]
    public async Task Runs_history_requires_admin_and_returns_array()
    {
        var anonymous = await Http.GetAsync("/api/demurrage/runs");
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var user = await CreateUserAsync();
        var byUser = await AuthedClient(user).GetAsync("/api/demurrage/runs");
        byUser.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var admin = await CreateAdminAsync();
        var byAdmin = await AuthedClient(admin).GetAsync("/api/demurrage/runs");
        byAdmin.StatusCode.Should().Be(HttpStatusCode.OK);
        var arr = await byAdmin.Content.ReadFromJsonAsync<JsonArray>();
        arr.Should().NotBeNull();
    }

    [Fact]
    public async Task Ipca_status_requires_admin_and_tolerates_bcb_outage()
    {
        var anonymous = await Http.GetAsync("/api/demurrage/ipca");
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var user = await CreateUserAsync();
        var byUser = await AuthedClient(user).GetAsync("/api/demurrage/ipca");
        byUser.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Admin vê 200 SEMPRE: IPCA indisponível (BCB fora) vira Accumulated/Adjusted null —
        // o reajuste adia, a taxa segue. A chamada externa é tolerada, não exigida.
        var admin = await CreateAdminAsync();
        var byAdmin = await AuthedClient(admin).GetAsync("/api/demurrage/ipca");
        byAdmin.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await byAdmin.Content.ReadFromJsonAsync<JsonNode>();
        json!["CurrentRateBps"]!.GetValue<int>().Should().BeGreaterThan(0);
        json["NextRunUtc"]!.GetValue<DateTime>().Should().BeAfter(DateTime.UtcNow);
    }
}
