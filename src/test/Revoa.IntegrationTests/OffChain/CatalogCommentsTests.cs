using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Catálogo (off-chain): comentários de anúncio + lacunas do controller. Feed/detalhe/categorias
// estão em CatalogFeedTests; aqui fechamos 404 de detalhe, 401/400 de criação (parse de enum no
// controller) e o fluxo de comentários raiz/resposta (100% Mongo, sem chain — usa anúncio
// Service que não minta ao listar).
public class CatalogCommentsTests : IntegrationTestBase
{
    public CatalogCommentsTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Listing_detail_unknown_returns_404_with_api_error()
    {
        var resp = await Http.GetAsync($"/api/listings/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["Error"]!.GetValue<string>().Should().Be("Anúncio não encontrado.");
    }

    [Fact]
    public async Task Create_listing_requires_authentication()
    {
        var resp = await Http.PostAsync("/api/listings", JsonBody(new
        {
            Kind = "Service",
            Mode = "Trade",
            Title = "Anônimo não cria",
            Description = "descrição",
            Imagens = Array.Empty<string>(),
            PriceRvm = 5L,
            CategoryId = await GetFirstCategoryIdAsync(),
            Visibility = "Global",
        }));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_listing_invalid_enum_returns_400_api_error()
    {
        var user = await CreateUserAsync();

        var badKind = await AuthedClient(user).PostAsync("/api/listings", JsonBody(new
        {
            Kind = "Imóvel", // parse de enum no controller → 400 antes do handler
            Mode = "Trade",
            Title = "Kind inválido E2E",
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
        badKind.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var kindJson = await badKind.Content.ReadFromJsonAsync<JsonNode>();
        kindJson!["Error"]!.GetValue<string>().Should().Be("Kind inválido (Product|Service).");

        var badMode = await AuthedClient(user).PostAsync("/api/listings", JsonBody(new
        {
            Kind = "Service",
            Mode = "Alugar",
            Title = "Modo inválido E2E",
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
        badMode.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var modeJson = await badMode.Content.ReadFromJsonAsync<JsonNode>();
        modeJson!["Error"]!.GetValue<string>().Should().Be("Modo inválido (Trade|Resell|Donate|Volunteer).");
    }

    [Fact]
    public async Task Comment_flow_root_and_reply_with_pascal_case_shape()
    {
        var seller = await CreateUserAsync();
        var listingId = await CreateServiceListingAsync(seller);
        var commenter = await CreateUserAsync();
        var client = AuthedClient(commenter);

        // Comentário raiz (gate Verified; autor vem do token).
        var root = await client.PostAsync($"/api/listings/{listingId}/comments", JsonBody(new
        {
            ParentId = (Guid?)null,
            Content = "Comentário raiz E2E",
        }));
        root.StatusCode.Should().Be(HttpStatusCode.OK);
        var rootId = await ReadIdAsync(root);

        // Resposta pendurada na raiz (thread recursiva, depth 2).
        var reply = await client.PostAsync($"/api/listings/{listingId}/comments", JsonBody(new
        {
            ParentId = rootId,
            Content = "Resposta E2E",
        }));
        reply.StatusCode.Should().Be(HttpStatusCode.OK);
        var replyId = await ReadIdAsync(reply);

        // Resposta apontando para pai de OUTRO anúncio → 400.
        var otherListing = await CreateServiceListingAsync(await CreateUserAsync());
        var crossParent = await client.PostAsync($"/api/listings/{otherListing}/comments", JsonBody(new
        {
            ParentId = rootId,
            Content = "Pai errado E2E",
        }));
        crossParent.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var crossJson = await crossParent.Content.ReadFromJsonAsync<JsonNode>();
        crossJson!["Error"]!.GetValue<string>()
            .Should().Be("Comentário pai não encontrado neste anúncio.");

        // Listagem anônima: raiz aparece; shape PascalCase (AutorId/Path/Depth do aggregate).
        var comments = await Http.GetAsync($"/api/listings/{listingId}/comments");
        comments.StatusCode.Should().Be(HttpStatusCode.OK);
        var arr = await comments.Content.ReadFromJsonAsync<JsonArray>();
        var listedRoot = arr!.Single(c => c!["Id"]!.GetValue<Guid>() == rootId)!;
        listedRoot["ListingId"]!.GetValue<Guid>().Should().Be(listingId);
        listedRoot["AutorId"]!.GetValue<Guid>().Should().Be(commenter.UserId);
        listedRoot["Content"]!.GetValue<string>().Should().Be("Comentário raiz E2E");
        listedRoot["ParentId"].Should().BeNull();
        listedRoot["Depth"]!.GetValue<int>().Should().Be(0);
        listedRoot["Status"]!.GetValue<string>().Should().Be("Visible");

        // parentId filtra respostas diretas da raiz.
        var children = await Http.GetAsync($"/api/listings/{listingId}/comments?parentId={rootId}");
        children.StatusCode.Should().Be(HttpStatusCode.OK);
        var childArr = await children.Content.ReadFromJsonAsync<JsonArray>();
        childArr!.Select(c => c!["Id"]!.GetValue<Guid>())
            .Should().ContainSingle().Which.Should().Be(replyId);

        // Comentário exige autenticação.
        var anonymous = await Http.PostAsync($"/api/listings/{listingId}/comments", JsonBody(new
        {
            ParentId = (Guid?)null,
            Content = "anônimo não comenta",
        }));
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<Guid> CreateServiceListingAsync(TestUser owner)
    {
        var resp = await AuthedClient(owner).PostAsync("/api/listings", JsonBody(new
        {
            Kind = "Service",
            Mode = "Trade",
            Title = "Serviço comentado E2E",
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
        resp.IsSuccessStatusCode.Should().BeTrue(
            $"esperado 2xx ao criar listing: {await resp.Content.ReadAsStringAsync()}");
        return await ReadIdAsync(resp);
    }
}
