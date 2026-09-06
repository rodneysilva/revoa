using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Busca global (GET /api/search?q=): anúncios + comunidades agrupados num
// pedido só — o que o dropdown do header consome.
public class SearchTests : IntegrationTestBase
{
    public SearchTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Short_term_returns_empty_groups()
    {
        var resp = await Http.GetAsync("/api/search?q=x");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["Listings"]!.AsArray().Should().BeEmpty();
        json["Communities"]!.AsArray().Should().BeEmpty();
    }

    [Fact]
    public async Task Search_finds_listings_and_communities_grouped()
    {
        var user = await CreateUserAsync();
        var client = AuthedClient(user);
        var categoryId = await GetFirstCategoryIdAsync();

        var listing = await client.PostAsync("/api/listings", JsonBody(new
        {
            Kind = "Service", // serviço não minta ao listar (mint-on-purchase) → off-chain seguro
            Mode = "Trade",
            Title = "Bicicleta — aula de manutenção urbana",
            Description = "Urban bike",
            Imagens = Array.Empty<string>(),
            PriceRvm = 100L,
            Lat = (double?)null,
            Lng = (double?)null,
            Neighborhood = (string?)null,
            City = (string?)null,
            PostalCode = (string?)null,
            CategoryId = categoryId,
            CommunityId = (Guid?)null,
            Visibility = "Global",
            Condition = (string?)null,
            Stock = (int?)null,
            UnitType = "PerService",
            Duration = 1,
            VoucherExpiryDays = 30,
        }));
        listing.IsSuccessStatusCode.Should().BeTrue(
            $"{await listing.Content.ReadAsStringAsync()}");

        var community = await client.PostAsync("/api/communities", JsonBody(new
        {
            Name = "Ciclistas do Centro",
            Description = "Grupo de bike",
            Type = "User",
            Axis = "Interest",
            Visibility = "Open",
        }));
        community.IsSuccessStatusCode.Should().BeTrue(
            $"{await community.Content.ReadAsStringAsync()}");

        // Termo comum aos dois mundos ("cicl" ⊂ Bicicleta e Ciclistas), case-insensitive.
        var resp = await Http.GetAsync("/api/search?q=CICL");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        var listings = json!["Listings"]!.AsArray();
        var communities = json["Communities"]!.AsArray();

        listings.Should().NotBeEmpty("o anúncio 'Bicicleta…' casa com o termo");
        listings[0]!["Title"]!.GetValue<string>().Should().Contain("Bicicleta");

        communities.Should().NotBeEmpty("a comunidade 'Ciclistas do Centro' casa com o termo");
        communities[0]!["Name"]!.GetValue<string>().Should().Contain("Ciclistas");
    }
}
