using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Revoa.Exchange.Domain.Aggregates.TradeAggregate;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Trocas/escrow (off-chain, sempre verde). O lifecycle completo (purchase→release/cancel) move
// fundos on-chain e é coberto em OnChain/TradeTests; aqui cobrimos o que é off-chain: leitura
// anônima (detalhe/histórico/help queue), gates 401/403 e validações 400 (envelope ApiError) de
// estado/ownership que o handler avalia ANTES de tocar a chain. Trades em estados arbitrários são
// seedadas direto no Mongo (OffChainSeed) — a criação real exige escrow on-chain.
public class ExchangeTests : IntegrationTestBase
{
    public ExchangeTests(ApiFactory factory) : base(factory) { }

    // ---- Leitura anônima ----

    [Fact]
    public async Task GetById_unknown_trade_returns_404_with_api_error()
    {
        var resp = await Http.GetAsync($"/api/trades/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["Error"]!.GetValue<string>().Should().Be("Troca não encontrada.");
    }

    [Fact]
    public async Task GetById_returns_pascal_case_summary_for_seeded_trade()
    {
        var seller = await CreateUserAsync();
        var buyer = await CreateUserAsync();
        var trade = await OffChainSeed.InsertTradeAsync(
            Factory, seller.UserId, buyer.UserId, TradeKind.Service, TradeState.Released);

        var resp = await Http.GetAsync($"/api/trades/{trade.Id}"); // anônimo vê (UF-11/12)

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["Id"]!.GetValue<Guid>().Should().Be(trade.Id);
        json["ListingId"]!.GetValue<Guid>().Should().Be(trade.ListingId);
        json["Mode"]!.GetValue<string>().Should().Be("Trade");
        json["Kind"]!.GetValue<string>().Should().Be("Service");
        json["SellerId"]!.GetValue<Guid>().Should().Be(seller.UserId);
        json["SellerName"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
        json["BuyerId"]!.GetValue<Guid>().Should().Be(buyer.UserId);
        json["TotalRvm"]!.GetValue<long>().Should().Be(10);
        json["State"]!.GetValue<string>().Should().Be("Released");
        json["VoucherRedeemed"]!.GetValue<bool>().Should().BeFalse();
        json["IsDonation"]!.GetValue<bool>().Should().BeFalse();

        // Contrato: o summary público NÃO expõe carteiras nem dados on-chain.
        var obj = json.AsObject();
        obj.Should().NotContainKey("SellerWallet");
        obj.Should().NotContainKey("BuyerWallet");
        obj.Should().NotContainKey("AssetContract");
        obj.Should().NotContainKey("TokenId");
        obj.Should().NotContainKey("OnChainTradeId");
        obj.Should().NotContainKey("LastTxHash");
    }

    [Fact]
    public async Task History_filters_by_seller_and_buyer_anonymously()
    {
        var seller = await CreateUserAsync();
        var buyer = await CreateUserAsync();
        var trade = await OffChainSeed.InsertTradeAsync(Factory, seller.UserId, buyer.UserId);

        var bySeller = await Http.GetAsync($"/api/trades?sellerId={seller.UserId}&page=1");
        bySeller.StatusCode.Should().Be(HttpStatusCode.OK);
        var sellerArr = await bySeller.Content.ReadFromJsonAsync<JsonArray>();
        sellerArr!.Select(t => t!["Id"]!.GetValue<Guid>()).Should().Contain(trade.Id);

        var byBuyer = await Http.GetAsync($"/api/trades?buyerId={buyer.UserId}");
        byBuyer.StatusCode.Should().Be(HttpStatusCode.OK);
        var buyerArr = await byBuyer.Content.ReadFromJsonAsync<JsonArray>();
        buyerArr!.Select(t => t!["Id"]!.GetValue<Guid>()).Should().Contain(trade.Id);
    }

    // ---- Gates de autenticação ----

    [Fact]
    public async Task Action_endpoints_require_authentication()
    {
        var id = Guid.NewGuid();

        var purchase = await Http.PostAsync("/api/trades/purchase", JsonBody(new { ListingId = id }));
        purchase.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var release = await Http.PostAsync($"/api/trades/{id}/release", content: null);
        release.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var cancel = await Http.PostAsync($"/api/trades/{id}/cancel", content: null);
        cancel.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var dispute = await Http.PostAsync($"/api/trades/{id}/dispute", content: null);
        dispute.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var redeem = await Http.PostAsync($"/api/trades/{id}/redeem", content: null);
        redeem.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var help = await Http.PostAsync("/api/help", JsonBody(new { ListingId = id, Message = "preciso" }));
        help.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---- Purchase: validação off-chain ----

    [Fact]
    public async Task Purchase_unknown_listing_returns_400_api_error()
    {
        var buyer = await CreateUserAsync();

        var resp = await AuthedClient(buyer).PostAsync(
            "/api/trades/purchase", JsonBody(new { ListingId = Guid.NewGuid() }));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["Error"]!.GetValue<string>().Should().Be("Anúncio não encontrado.");
    }

    [Fact]
    public async Task Purchase_donation_listing_routes_to_help_queue()
    {
        var donor = await CreateUserAsync();
        var listingId = await CreateVolunteerListingAsync(donor);

        var buyer = await CreateUserAsync();
        var resp = await AuthedClient(buyer).PostAsync(
            "/api/trades/purchase", JsonBody(new { ListingId = listingId }));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["Error"]!.GetValue<string>()
            .Should().Be("Anúncio de doação/voluntariado usa a fila de ajuda.");
    }

    // ---- Release/Cancel/Dispute/Resolve/Redeem: validação off-chain (antes da chain) ----

    [Fact]
    public async Task Release_rejects_unknown_trade_and_non_party()
    {
        var seller = await CreateUserAsync();
        var buyer = await CreateUserAsync();
        var outsider = await CreateUserAsync();
        var trade = await OffChainSeed.InsertTradeAsync(Factory, seller.UserId, buyer.UserId);

        var unknown = await AuthedClient(seller).PostAsync($"/api/trades/{Guid.NewGuid()}/release", content: null);
        unknown.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var unknownJson = await unknown.Content.ReadFromJsonAsync<JsonNode>();
        unknownJson!["Error"]!.GetValue<string>().Should().Be("Troca não encontrada.");

        // Não-parte (nem vendedor, nem comprador) não libera — validado antes da chain.
        var byOutsider = await AuthedClient(outsider).PostAsync($"/api/trades/{trade.Id}/release", content: null);
        byOutsider.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var outsiderJson = await byOutsider.Content.ReadFromJsonAsync<JsonNode>();
        outsiderJson!["Error"]!.GetValue<string>()
            .Should().Be("Apenas vendedor ou comprador podem liberar a troca.");
    }

    [Fact]
    public async Task Release_on_released_trade_returns_400_wrong_state()
    {
        var seller = await CreateUserAsync();
        var buyer = await CreateUserAsync();
        var trade = await OffChainSeed.InsertTradeAsync(
            Factory, seller.UserId, buyer.UserId, TradeKind.Service, TradeState.Released);

        // Máquina de estados: só Funded/Disputed pode liberar (guarda ANTES da chain).
        var resp = await AuthedClient(seller).PostAsync($"/api/trades/{trade.Id}/release", content: null);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["Error"]!.GetValue<string>().Should().Be("Troca não está em estado de liberação.");
    }

    [Fact]
    public async Task Cancel_rejects_unknown_trade_and_non_party()
    {
        var seller = await CreateUserAsync();
        var buyer = await CreateUserAsync();
        var outsider = await CreateUserAsync();
        var trade = await OffChainSeed.InsertTradeAsync(Factory, seller.UserId, buyer.UserId);

        var unknown = await AuthedClient(seller).PostAsync($"/api/trades/{Guid.NewGuid()}/cancel", content: null);
        unknown.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var unknownJson = await unknown.Content.ReadFromJsonAsync<JsonNode>();
        unknownJson!["Error"]!.GetValue<string>().Should().Be("Troca não encontrada.");

        var byOutsider = await AuthedClient(outsider).PostAsync($"/api/trades/{trade.Id}/cancel", content: null);
        byOutsider.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var outsiderJson = await byOutsider.Content.ReadFromJsonAsync<JsonNode>();
        outsiderJson!["Error"]!.GetValue<string>()
            .Should().Be("Apenas vendedor ou comprador podem cancelar a troca.");
    }

    [Fact]
    public async Task Dispute_rejects_unknown_trade_and_non_party()
    {
        var seller = await CreateUserAsync();
        var buyer = await CreateUserAsync();
        var outsider = await CreateUserAsync();
        var trade = await OffChainSeed.InsertTradeAsync(Factory, seller.UserId, buyer.UserId);

        var unknown = await AuthedClient(seller).PostAsync($"/api/trades/{Guid.NewGuid()}/dispute", content: null);
        unknown.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var unknownJson = await unknown.Content.ReadFromJsonAsync<JsonNode>();
        unknownJson!["Error"]!.GetValue<string>().Should().Be("Troca não encontrada.");

        var byOutsider = await AuthedClient(outsider).PostAsync($"/api/trades/{trade.Id}/dispute", content: null);
        byOutsider.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var outsiderJson = await byOutsider.Content.ReadFromJsonAsync<JsonNode>();
        outsiderJson!["Error"]!.GetValue<string>()
            .Should().Be("Apenas vendedor ou comprador podem abrir disputa.");
    }

    [Fact]
    public async Task Resolve_requires_disputed_state()
    {
        // Complementa ResolveAuthzTests (403 comum / role Árbitro): admin resolve uma trade
        // NÃO disputada → 400 de estado (off-chain, antes da chain).
        var admin = await CreateAdminAsync();
        var seller = await CreateUserAsync();
        var buyer = await CreateUserAsync();
        var trade = await OffChainSeed.InsertTradeAsync(Factory, seller.UserId, buyer.UserId);

        var resp = await AuthedClient(admin).PostAsync(
            $"/api/trades/{trade.Id}/resolve", JsonBody(new { ReleaseToSeller = true }));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["Error"]!.GetValue<string>()
            .Should().Be("Apenas troca disputada pode ser resolvida por árbitro.");
    }

    [Fact]
    public async Task Redeem_validates_existence_kind_and_state()
    {
        var seller = await CreateUserAsync();
        var buyer = await CreateUserAsync();

        // Inexistente → 400.
        var unknown = await AuthedClient(buyer).PostAsync($"/api/trades/{Guid.NewGuid()}/redeem", content: null);
        unknown.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var unknownJson = await unknown.Content.ReadFromJsonAsync<JsonNode>();
        unknownJson!["Error"]!.GetValue<string>().Should().Be("Troca não encontrada.");

        // Produto → redeem não se aplica (voucher é só de serviço).
        var product = await OffChainSeed.InsertTradeAsync(Factory, seller.UserId, buyer.UserId, TradeKind.Product);
        var onProduct = await AuthedClient(buyer).PostAsync($"/api/trades/{product.Id}/redeem", content: null);
        onProduct.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var productJson = await onProduct.Content.ReadFromJsonAsync<JsonNode>();
        productJson!["Error"]!.GetValue<string>().Should().Be("Redeem aplica apenas a serviços.");

        // Serviço já liberado → só trade financiada permite redeem.
        var released = await OffChainSeed.InsertTradeAsync(
            Factory, seller.UserId, buyer.UserId, TradeKind.Service, TradeState.Released);
        var onReleased = await AuthedClient(buyer).PostAsync($"/api/trades/{released.Id}/redeem", content: null);
        onReleased.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var releasedJson = await onReleased.Content.ReadFromJsonAsync<JsonNode>();
        releasedJson!["Error"]!.GetValue<string>().Should().Be("Apenas troca financiada permite redeem.");
    }

    // ---- Fila de ajuda (doação/voluntariado) — 100% off-chain com anúncio Service ----

    [Fact]
    public async Task Help_request_appears_in_anonymous_queue()
    {
        var donor = await CreateUserAsync();
        var listingId = await CreateVolunteerListingAsync(donor);

        var receiver = await CreateUserAsync();
        var request = await AuthedClient(receiver).PostAsync("/api/help", JsonBody(new
        {
            ListingId = listingId,
            Message = "Preciso de ajuda E2E",
        }));
        request.StatusCode.Should().Be(HttpStatusCode.Created);
        var helpRequestId = await ReadIdAsync(request);
        helpRequestId.Should().NotBeEmpty();

        // Segundo usuário também entra na fila.
        var other = await CreateUserAsync();
        var second = await AuthedClient(other).PostAsync("/api/help", JsonBody(new
        {
            ListingId = listingId,
            Message = "Eu também preciso E2E",
        }));
        second.StatusCode.Should().Be(HttpStatusCode.Created);

        // Fila anônima (OOUX 13): 2 pedidos abertos, shape PascalCase.
        var queue = await Http.GetAsync($"/api/help?listingId={listingId}");
        queue.StatusCode.Should().Be(HttpStatusCode.OK);
        var arr = await queue.Content.ReadFromJsonAsync<JsonArray>();
        arr!.Should().HaveCount(2);
        var first = arr.Single(h => h!["Id"]!.GetValue<Guid>() == helpRequestId)!;
        first["ListingId"]!.GetValue<Guid>().Should().Be(listingId);
        first["AuthorId"]!.GetValue<Guid>().Should().Be(receiver.UserId);
        first["Message"]!.GetValue<string>().Should().Be("Preciso de ajuda E2E");
        first["State"]!.GetValue<string>().Should().Be("Open");
        first["SelectedTradeId"].Should().BeNull();
    }

    [Fact]
    public async Task Help_request_empty_message_returns_validation_errors()
    {
        var donor = await CreateUserAsync();
        var listingId = await CreateVolunteerListingAsync(donor);
        var user = await CreateUserAsync();

        var resp = await AuthedClient(user).PostAsync("/api/help", JsonBody(new
        {
            ListingId = listingId,
            Message = "",
        }));

        // Envelope de validação do pipeline: Error + Errors[{Field, Message}].
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["Error"]!.GetValue<string>().Should().Be("Dados inválidos.");
        var errors = json["Errors"]!.AsArray();
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e!["Field"]!.GetValue<string>() == "Message");
    }

    [Fact]
    public async Task Help_on_non_donation_listing_returns_400()
    {
        var seller = await CreateUserAsync();
        var listingId = await CreateServiceListingAsync(seller, mode: "Trade", priceRvm: 5);
        var user = await CreateUserAsync();

        var resp = await AuthedClient(user).PostAsync("/api/help", JsonBody(new
        {
            ListingId = listingId,
            Message = "Quero comprar, não pedir ajuda",
        }));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["Error"]!.GetValue<string>()
            .Should().Be("Apenas anúncios de doação/voluntariado aceitam pedidos de ajuda.");
    }

    [Fact]
    public async Task Select_recipient_rejects_unknown_request_and_non_donor()
    {
        var donor = await CreateUserAsync();
        var listingId = await CreateVolunteerListingAsync(donor);

        var receiver = await CreateUserAsync();
        var request = await AuthedClient(receiver).PostAsync("/api/help", JsonBody(new
        {
            ListingId = listingId,
            Message = "Selecione-me E2E",
        }));
        var helpRequestId = await ReadIdAsync(request);
        request.StatusCode.Should().Be(HttpStatusCode.Created);

        var unknown = await AuthedClient(donor).PostAsync($"/api/help/{Guid.NewGuid()}/select", content: null);
        unknown.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var unknownJson = await unknown.Content.ReadFromJsonAsync<JsonNode>();
        unknownJson!["Error"]!.GetValue<string>().Should().Be("Pedido de ajuda não encontrado.");

        // Só o dono do anúncio (doador) seleciona — o próprio receptor não pode.
        var byReceiver = await AuthedClient(receiver).PostAsync($"/api/help/{helpRequestId}/select", content: null);
        byReceiver.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var receiverJson = await byReceiver.Content.ReadFromJsonAsync<JsonNode>();
        receiverJson!["Error"]!.GetValue<string>()
            .Should().Be("Apenas o doador pode selecionar o receptor.");
    }

    // ---- Helpers ----

    // Cria anúncio Service (NÃO minta ao listar → off-chain seguro). Mode Volunteer + preço 0
    // para a fila de ajuda; Mode Trade + preço > 0 para fluxo de compra.
    private async Task<Guid> CreateServiceListingAsync(TestUser owner, string mode, long priceRvm)
    {
        var resp = await AuthedClient(owner).PostAsync("/api/listings", JsonBody(new
        {
            Kind = "Service",
            Mode = mode,
            Title = mode == "Volunteer" ? "Voluntariado E2E" : "Serviço E2E",
            Description = "Descrição E2E off-chain",
            Imagens = Array.Empty<string>(),
            PriceRvm = priceRvm,
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

    private Task<Guid> CreateVolunteerListingAsync(TestUser owner)
        => CreateServiceListingAsync(owner, mode: "Volunteer", priceRvm: 0);
}
