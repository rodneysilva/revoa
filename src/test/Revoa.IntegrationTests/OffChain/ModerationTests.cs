using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Moderação (off-chain, admin): denúncia de usuário verificado → listagem admin → resolução
// (Dismissed). Denúncia alvo de Listing (anúncio Service, sem chain).
public class ModerationTests : IntegrationTestBase
{
    public ModerationTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Report_then_admin_lists_and_resolves_as_dismissed()
    {
        // Repórter cria um anúncio (alvo da denúncia).
        var reporter = await CreateUserAsync();
        var categoriaId = await GetFirstCategoryIdAsync();
        var reporterClient = AuthedClient(reporter);

        var createListing = await reporterClient.PostAsync("/api/listings", JsonBody(new
        {
            Kind = "Service",
            Mode = "Trade",
            Title = "Alvo denúncia E2E",
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
            Condition = (string?)null,
            Stock = (int?)null,
            UnitType = "PerService",
            Duration = 1,
            VoucherExpiryDays = 30,
        }));
        createListing.IsSuccessStatusCode.Should().BeTrue(
            $"esperado 2xx ao criar listing alvo: {await createListing.Content.ReadAsStringAsync()}");
        var listingId = await ReadIdAsync(createListing);

        // Denúncia (gate Verified).
        var reportResp = await reporterClient.PostAsync("/api/reports", JsonBody(new
        {
            TargetType = "Listing",
            TargetId = listingId.ToString(),
            Reason = "Spam",
            Details = (string?)"Denúncia E2E",
        }));
        reportResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var reportId = await ReadIdAsync(reportResp);

        // Admin lista denúncias → contém a criada.
        var admin = await CreateAdminAsync();
        var adminClient = AuthedClient(admin);

        var list = await adminClient.GetAsync("/api/reports");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var listArr = await list.Content.ReadFromJsonAsync<JsonArray>();
        listArr!.Select(r => r!["Id"]!.GetValue<Guid>()).Should().Contain(reportId);

        // Admin resolve como Dismissed.
        var resolve = await adminClient.PostAsync($"/api/reports/{reportId}/resolve", JsonBody(new
        {
            Action = "Dismissed",
            Note = (string?)"Resolvido E2E",
        }));
        resolve.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
