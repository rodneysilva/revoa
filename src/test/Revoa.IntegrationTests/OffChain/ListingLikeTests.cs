using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Curtir anúncio (POST /api/listings/{id}/like) + bootstrap liked/ids. O card
// de anúncio na comunidade se comporta como um post — curtir é interação leve,
// sem gate de membership. Kind=Service para não depender de mint on-chain.
public class ListingLikeTests : IntegrationTestBase
{
    public ListingLikeTests(ApiFactory factory) : base(factory) { }

    private async Task<Guid> CreateListingAsync(HttpClient client, Guid categoryId, string title)
    {
        var resp = await client.PostAsync("/api/listings", JsonBody(new
        {
            Kind = "Service",
            Mode = "Trade",
            Title = title,
            Description = "descrição",
            Imagens = Array.Empty<string>(),
            PriceRvm = 5L,
            CategoryId = categoryId,
            CommunityId = (Guid?)null,
            Visibility = "Global",
            UnitType = "PerService",
            Duration = 1,
            VoucherExpiryDays = 30,
        }));
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        return await ReadIdAsync(resp);
    }

    [Fact]
    public async Task Like_requires_auth()
    {
        var seller = await CreateUserAsync();
        var listingId = await CreateListingAsync(
            AuthedClient(seller), await GetFirstCategoryIdAsync(), "Anúncio p/ curtir");

        var anon = await Http.PostAsync($"/api/listings/{listingId}/like", null);
        anon.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var ids = await Http.GetAsync("/api/listings/liked/ids");
        ids.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Toggle_like_roundtrip_feeds_liked_ids()
    {
        var seller = await CreateUserAsync();
        var liker = AuthedClient(await CreateUserAsync());
        var listingId = await CreateListingAsync(
            AuthedClient(seller), await GetFirstCategoryIdAsync(), "Anúncio curtível");

        var on = await liker.PostAsync($"/api/listings/{listingId}/like", null);
        on.StatusCode.Should().Be(HttpStatusCode.OK);
        (await on.Content.ReadFromJsonAsync<JsonNode>())!["Liked"]!.GetValue<bool>()
            .Should().BeTrue();

        var ids = await liker.GetFromJsonAsync<IReadOnlyList<Guid>>("/api/listings/liked/ids");
        ids.Should().Contain(listingId);

        // Toggle de novo = descurta (idempotente).
        var off = await liker.PostAsync($"/api/listings/{listingId}/like", null);
        (await off.Content.ReadFromJsonAsync<JsonNode>())!["Liked"]!.GetValue<bool>()
            .Should().BeFalse();

        var idsDepois = await liker.GetFromJsonAsync<IReadOnlyList<Guid>>("/api/listings/liked/ids");
        idsDepois.Should().NotContain(listingId);
    }

    [Fact]
    public async Task Likes_are_private_per_user()
    {
        var seller = await CreateUserAsync();
        var listingId = await CreateListingAsync(
            AuthedClient(seller), await GetFirstCategoryIdAsync(), "Anúncio de dois");

        var a = AuthedClient(await CreateUserAsync());
        var b = AuthedClient(await CreateUserAsync());
        await a.PostAsync($"/api/listings/{listingId}/like", null);

        var idsB = await b.GetFromJsonAsync<IReadOnlyList<Guid>>("/api/listings/liked/ids");
        idsB.Should().NotContain(listingId, "curtida de A não vira estado de B");
    }

    [Fact]
    public async Task Like_unknown_listing_returns_404()
    {
        var liker = AuthedClient(await CreateUserAsync());
        var resp = await liker.PostAsync($"/api/listings/{Guid.NewGuid()}/like", null);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
