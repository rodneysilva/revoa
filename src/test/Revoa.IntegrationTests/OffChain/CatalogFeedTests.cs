using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Catálogo (off-chain, sempre verde). Usa anúncio do tipo Service (mint-on-purchase, não minta ao
// listar) para não depender da chain — criar Product faria mint-to-escrow on-chain e exigiria anvil.
public class CatalogFeedTests : IntegrationTestBase
{
    public CatalogFeedTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Categories_seed_returns_at_least_nine()
    {
        var resp = await Http.GetAsync("/api/categories");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var arr = await resp.Content.ReadFromJsonAsync<JsonArray>();
        arr.Should().NotBeNull();
        arr!.Count.Should().BeGreaterThanOrEqualTo(9);
    }

    [Fact]
    public async Task Feed_returns_ok_shape()
    {
        var resp = await Http.GetAsync("/api/listings/feed");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var arr = await resp.Content.ReadFromJsonAsync<JsonArray>();
        arr.Should().NotBeNull(); // pode estar vazio sem seed de listings — só valida o shape/200.
    }

    [Fact]
    public async Task Create_service_listing_and_get_detail()
    {
        var seller = await CreateUserAsync();
        var categoriaId = await GetFirstCategoryIdAsync();
        var client = AuthedClient(seller);

        var create = await client.PostAsync("/api/listings", JsonBody(new
        {
            Kind = "Service", // serviço NÃO minta ao listar (mint-on-purchase) → off-chain seguro
            Modo = "Trocar",
            Titulo = "Aula de violão E2E",
            Descricao = "Descrição E2E do serviço",
            Imagens = Array.Empty<string>(),
            PrecoRvm = 10L,
            Lat = (double?)null,
            Lng = (double?)null,
            Bairro = (string?)null,
            Cidade = (string?)null,
            Cep = (string?)null,
            CategoriaId = categoriaId,
            ComunidadeId = (Guid?)null,
            Visibilidade = "Global",
            Condition = (string?)null,
            Stock = (int?)null,
            UnitType = "Hours",
            Duration = 1,
            VoucherExpiryDays = 30,
        }));

        create.IsSuccessStatusCode.Should().BeTrue(
            $"esperado 2xx ao criar listing: {await create.Content.ReadAsStringAsync()}");
        var raw = await create.Content.ReadAsStringAsync();
        var listingId = Guid.Parse(raw.Trim('"'));

        var detail = await client.GetAsync($"/api/listings/{listingId}");
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await detail.Content.ReadFromJsonAsync<JsonNode>();
        json!["Status"]!.GetValue<string>().Should().Be("Ativo");
        json["Kind"]!.GetValue<string>().Should().Be("Service");
    }
}
