using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OnChain;

// Troca (escrow atomic swap, UF-11): A cria anúncio TROCAR produto → B compra → State Financiada →
// A libera → State Liberada. ON-CHAIN: pula graceful se o anvil + contratos não estiverem acessíveis.
public class TradeTests : IntegrationTestBase
{
    public TradeTests(ApiFactory factory) : base(factory) { }

    [SkippableFact]
    public async Task Purchase_funds_and_release_liberates_trade()
    {
        SkipIfChainUnavailable();

        var seller = await CreateUserAsync();
        var buyer = await CreateUserAsync();
        var categoriaId = await GetFirstCategoryIdAsync();
        var sellerClient = AuthedClient(seller);

        // A cria anúncio TROCAR produto (mint-to-escrow on-chain ao listar).
        var create = await sellerClient.PostAsync("/api/listings", JsonBody(new
        {
            Kind = "Product",
            Mode = "Trade",
            Title = "Produto Troca E2E",
            Description = "descrição",
            Imagens = Array.Empty<string>(),
            PriceRvm = 5L,
            Lat = (double?)null,
            Lng = (double?)null,
            Neighborhood = (string?)null,
            City = (string?)null,
            PostalCode = (string?)null,
            CategoryId = categoriaId,
            CommunityId = (Guid?)null,
            Visibility = "Global",
            Condition = "Usado",
            Stock = 1,
            UnitType = (string?)null,
            Duration = (int?)null,
            VoucherExpiryDays = (int?)null,
        }));
        var listingId = await create.Content.ReadFromJsonAsync<Guid>();

        // B compra → trade nasce financiada.
        var purchase = await AuthedClient(buyer).PostAsync("/api/trades/purchase", JsonBody(new
        {
            ListingId = listingId,
        }));
        purchase.StatusCode.Should().Be(HttpStatusCode.Created);
        var tradeId = await purchase.Content.ReadFromJsonAsync<Guid>();

        await AssertStateAsync(tradeId, "Funded");

        // A libera → State Liberada (atomic swap finalizado).
        var release = await sellerClient.PostAsync($"/api/trades/{tradeId}/release", content: null);
        release.StatusCode.Should().Be(HttpStatusCode.OK);

        await AssertStateAsync(tradeId, "Released");
    }

    private async Task AssertStateAsync(Guid tradeId, string expected)
    {
        var detail = await Http.GetAsync($"/api/trades/{tradeId}");
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await detail.Content.ReadFromJsonAsync<JsonNode>();
        json!["State"]!.GetValue<string>().Should().Be(expected);
    }
}
