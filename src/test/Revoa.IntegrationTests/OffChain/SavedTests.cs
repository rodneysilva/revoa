using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Anúncios salvos (bookmark pessoal): toggle idempotente + lista privada do
// usuário do token (GET /api/listings/saved, saved/ids).
public class SavedTests : IntegrationTestBase
{
    public SavedTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Save_requires_auth()
    {
        var resp = await Http.PostAsync($"/api/listings/{Guid.NewGuid()}/save", null);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var lista = await Http.GetAsync("/api/listings/saved");
        lista.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Toggle_save_lists_and_removes()
    {
        var user = await CreateUserAsync();
        var client = AuthedClient(user);
        var listingId = await CreateServiceListingAsync(client);

        // Salva → true, aparece na lista e nos ids.
        var save = await client.PostAsync($"/api/listings/{listingId}/save", null);
        save.StatusCode.Should().Be(HttpStatusCode.OK);
        var savedJson = await save.Content.ReadFromJsonAsync<JsonObject>();
        savedJson!["Saved"]!.GetValue<bool>().Should().BeTrue();

        var ids = await client.GetFromJsonAsync<JsonArray>("/api/listings/saved/ids");
        ids!.Select(i => i!.GetValue<Guid>()).Should().Contain(listingId);

        var lista = await client.GetFromJsonAsync<JsonArray>("/api/listings/saved");
        lista!.Should().HaveCount(1);
        lista[0]!["Id"]!.GetValue<Guid>().Should().Be(listingId);
        lista[0]!["Title"]!.GetValue<string>().Should().NotBeNullOrEmpty();

        // Dessalva → false, some da lista.
        var unsave = await client.PostAsync($"/api/listings/{listingId}/save", null);
        var unsavedJson = await unsave.Content.ReadFromJsonAsync<JsonObject>();
        unsavedJson!["Saved"]!.GetValue<bool>().Should().BeFalse();

        var apos = await client.GetFromJsonAsync<JsonArray>("/api/listings/saved");
        apos!.Should().BeEmpty();
    }

    [Fact]
    public async Task Save_unknown_listing_returns_404()
    {
        var user = await CreateUserAsync();
        var client = AuthedClient(user);

        var resp = await client.PostAsync($"/api/listings/{Guid.NewGuid()}/save", null);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Saved_list_is_private_per_user()
    {
        var dono = await CreateUserAsync();
        var outro = await CreateUserAsync();
        var donoClient = AuthedClient(dono);
        var outroClient = AuthedClient(outro);
        var listingId = await CreateServiceListingAsync(donoClient);

        var save = await donoClient.PostAsync($"/api/listings/{listingId}/save", null);
        save.IsSuccessStatusCode.Should().BeTrue();

        // O outro usuário não vê o bookmark alheio — a lista é do token.
        var listaOutro = await outroClient.GetFromJsonAsync<JsonArray>("/api/listings/saved");
        listaOutro!.Should().BeEmpty();
    }

    // Service listing (off-chain seguro: não minta NFT ao criar).
    private async Task<Guid> CreateServiceListingAsync(HttpClient client)
    {
        var resp = await client.PostAsync("/api/listings", JsonBody(new
        {
            Kind = "Service",
            Mode = "Trade",
            Title = "Serviço salvo E2E",
            Description = "descrição",
            Imagens = Array.Empty<string>(),
            PriceRvm = 5L,
            Lat = (double?)null,
            Lng = (double?)null,
            Neighborhood = (string?)null,
            City = (string?)null,
            PostalCode = (string?)null,
            CategoryId = await GetFirstCategoryIdAsync(),
            CommunityId = (Guid?)null,
            Visibility = "Global",
            Condition = (string?)null,
            Stock = (int?)null,
            UnitType = "PerService",
            Duration = 1,
            VoucherExpiryDays = 30,
        }));
        resp.IsSuccessStatusCode.Should().BeTrue($"{await resp.Content.ReadAsStringAsync()}");
        return await ReadIdAsync(resp);
    }
}
