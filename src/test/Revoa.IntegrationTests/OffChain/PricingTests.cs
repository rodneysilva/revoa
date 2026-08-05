using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Pricing Intelligence (off-chain, admin). Sem anúncios ativos (db resetado) o refresh retorna 0
// sem chamar IBGE/Ollama — resiliência garantida pelo handler. Leitura anônima retorna a lista.
public class PricingTests : IntegrationTestBase
{
    public PricingTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Refresh_returns_non_negative_and_get_all_lists()
    {
        var admin = await CreateAdminAsync();
        var adminClient = AuthedClient(admin);

        var refresh = await adminClient.PostAsync("/api/pricing/refresh", content: null);
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshJson = await refresh.Content.ReadFromJsonAsync<JsonNode>();
        refreshJson!["updated"]!.GetValue<int>().Should().BeGreaterThanOrEqualTo(0);

        // Leitura anônima (transparência) → lista (pode estar vazia).
        var all = await Http.GetAsync("/api/pricing");
        all.StatusCode.Should().Be(HttpStatusCode.OK);
        var allArr = await all.Content.ReadFromJsonAsync<JsonArray>();
        allArr.Should().NotBeNull();
    }
}
