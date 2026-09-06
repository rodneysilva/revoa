using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OnChain;

// Doação (UF-16): A cria anúncio DOAR → B pede → A seleciona receptor → A libera → reputação de A
// reflete a doação (DonationsCount≥1, Points>0). ON-CHAIN: pula graceful se anvil indisponível.
public class DonationTests : IntegrationTestBase
{
    public DonationTests(ApiFactory factory) : base(factory) { }

    [SkippableFact]
    public async Task Donation_flow_awards_reputation_to_donor()
    {
        SkipIfChainUnavailable();

        var donor = await CreateUserAsync();
        var receiver = await CreateUserAsync();
        var categoriaId = await GetFirstCategoryIdAsync();
        var donorClient = AuthedClient(donor);

        // A cria anúncio DOAR produto (mint-to-escrow on-chain ao listar).
        var create = await donorClient.PostAsync("/api/listings", JsonBody(new
        {
            Kind = "Product",
            Mode = "Donate",
            Title = "Item Doação E2E",
            Description = "descrição",
            Imagens = Array.Empty<string>(),
            PriceRvm = 0L,
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

        // B pede ajuda (entra na fila de doação).
        var request = await AuthedClient(receiver).PostAsync("/api/help", JsonBody(new
        {
            ListingId = listingId,
            Message = "Preciso deste item E2E",
        }));
        var helpRequestId = await request.Content.ReadFromJsonAsync<Guid>();

        // A seleciona o receptor → cria trade de doação (total=0, Funded).
        var select = await donorClient.PostAsync($"/api/help/{helpRequestId}/select", content: null);
        select.StatusCode.Should().Be(HttpStatusCode.Created);
        var tradeId = await select.Content.ReadFromJsonAsync<Guid>();

        // A libera → doação concluída (publica DonationCompletedEvent → reputação do doador).
        var release = await donorClient.PostAsync($"/api/trades/{tradeId}/release", content: null);
        release.StatusCode.Should().Be(HttpStatusCode.OK);

        // Reputação do doador (A) reflete a doação.
        var rep = await Http.GetAsync($"/api/users/{donor.UserId}/reputation");
        rep.StatusCode.Should().Be(HttpStatusCode.OK);
        var repJson = await rep.Content.ReadFromJsonAsync<JsonNode>();
        repJson!["DonationsCount"]!.GetValue<int>().Should().BeGreaterThanOrEqualTo(1);
        repJson["Points"]!.GetValue<long>().Should().BeGreaterThan(0);
    }
}
