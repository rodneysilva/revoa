using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Escopo de comunidade no anúncio (Visibility=Community + CommunityId):
// o gate de membership (IMembershipStatusChecker, porta cross-módulo) e o
// comportamento do feed — escopado aparece na comunidade e some do feed geral.
// Kind=Service para não depender de mint on-chain (produto minta NFT).
public class ListingScopeTests : IntegrationTestBase
{
    public ListingScopeTests(ApiFactory factory) : base(factory) { }

    private async Task<Guid> CreateCommunityAsync(HttpClient client, string name)
    {
        var resp = await client.PostAsync("/api/communities", JsonBody(new
        {
            Name = name,
            Description = "Comunidade p/ escopo de anúncio",
            Type = "User",
            Axis = "Interest",
            Visibility = "Open",
        }));
        return await ReadIdAsync(resp);
    }

    private static object ScopedBody(Guid communityId, Guid categoryId) => new
    {
        Kind = "Service",
        Mode = "Trade",
        Title = "Aula de violão na comunidade",
        Description = "descrição",
        Imagens = Array.Empty<string>(),
        PriceRvm = 5L,
        CategoryId = categoryId,
        CommunityId = communityId,
        Visibility = "Community",
        UnitType = "PerService",
        Duration = 1,
        VoucherExpiryDays = 30,
    };

    [Fact]
    public async Task Scoped_listing_requires_membership()
    {
        var creator = await CreateUserAsync();
        var outsider = await CreateUserAsync();

        var communityId = await CreateCommunityAsync(AuthedClient(creator), "Escopo Gate");

        var resp = await AuthedClient(outsider).PostAsync(
            "/api/listings", JsonBody(ScopedBody(communityId, await GetFirstCategoryIdAsync())));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["Error"]!.GetValue<string>().Should().Be("Você não participa desta comunidade.");
    }

    [Fact]
    public async Task Scoped_listing_shows_in_community_feed_and_hides_from_global()
    {
        var creator = await CreateUserAsync();
        var client = AuthedClient(creator);

        var communityId = await CreateCommunityAsync(client, "Escopo Feed");
        var create = await client.PostAsync(
            "/api/listings", JsonBody(ScopedBody(communityId, await GetFirstCategoryIdAsync())));
        create.StatusCode.Should().Be(HttpStatusCode.Created,
            "o criador da comunidade tem vínculo Active por definição");

        // Na comunidade: aparece (Global/Ambos + Community do CommunityId).
        var naComunidade = await Http.GetAsync($"/api/listings/feed?communityId={communityId}");
        var comunidadeJson = await naComunidade.Content.ReadFromJsonAsync<JsonArray>();
        comunidadeJson!
            .Should().ContainSingle(l => l!["Title"]!.GetValue<string>() == "Aula de violão na comunidade");

        // No feed geral: escopados ficam de fora (regra do repositório).
        var geral = await Http.GetAsync("/api/listings/feed");
        var geralJson = await geral.Content.ReadFromJsonAsync<JsonArray>();
        geralJson!
            .Should().NotContain(l => l!["Title"]!.GetValue<string>() == "Aula de violão na comunidade");
    }
}
