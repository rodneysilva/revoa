using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Revoa.Exchange.Domain.Aggregates.TradeAggregate;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Avaliações pós-troca (off-chain, sempre verde). Criar uma avaliação exige uma trade Liberada —
// via HTTP isso só acontece depois do release on-chain — então a trade é seedada direto no Mongo
// (OffChainSeed) e o fluxo inteiro de review (criar/duplicar/parte/rating + reputação) roda
// off-chain: o evento ReviewSubmitted só toca o Mongo.
public class ReviewsTests : IntegrationTestBase
{
    public ReviewsTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task List_reviews_is_anonymous_and_returns_empty_for_new_user()
    {
        var user = await CreateUserAsync();

        var resp = await Http.GetAsync($"/api/users/{user.UserId}/reviews");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var arr = await resp.Content.ReadFromJsonAsync<JsonArray>();
        arr.Should().NotBeNull();
        arr!.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_review_requires_authentication()
    {
        var resp = await Http.PostAsync(
            $"/api/trades/{Guid.NewGuid()}/reviews", JsonBody(new { Rating = 5, Comment = (string?)"Ótimo" }));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_review_unknown_trade_returns_400_api_error()
    {
        var user = await CreateUserAsync();

        var resp = await AuthedClient(user).PostAsync(
            $"/api/trades/{Guid.NewGuid()}/reviews", JsonBody(new { Rating = 5, Comment = (string?)"Ótimo" }));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["Error"]!.GetValue<string>().Should().Be("Troca não encontrada.");
    }

    [Fact]
    public async Task Review_flow_on_released_trade_updates_public_profile_and_reputation()
    {
        var seller = await CreateUserAsync();
        var buyer = await CreateUserAsync();
        var trade = await OffChainSeed.InsertTradeAsync(
            Factory, seller.UserId, buyer.UserId, TradeKind.Service, TradeState.Released);

        // Vendedor avalia a contraparte (reviewee derivado do trade, nunca do body).
        var create = await AuthedClient(seller).PostAsync(
            $"/api/trades/{trade.Id}/reviews", JsonBody(new { Rating = 5, Comment = "Comprador pontual" }));
        create.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviewId = await ReadIdAsync(create); // envelope {"Id": "..."}
        reviewId.Should().NotBeEmpty();

        // Duplicada do mesmo reviewer → 400.
        var duplicate = await AuthedClient(seller).PostAsync(
            $"/api/trades/{trade.Id}/reviews", JsonBody(new { Rating = 4, Comment = (string?)"de novo" }));
        duplicate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var dupJson = await duplicate.Content.ReadFromJsonAsync<JsonNode>();
        dupJson!["Error"]!.GetValue<string>().Should().Be("Você já avaliou esta troca.");

        // Terceiro não é parte da troca → 400.
        var outsider = await CreateUserAsync();
        var byOutsider = await AuthedClient(outsider).PostAsync(
            $"/api/trades/{trade.Id}/reviews", JsonBody(new { Rating = 5, Comment = (string?)"hi" }));
        byOutsider.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var outsiderJson = await byOutsider.Content.ReadFromJsonAsync<JsonNode>();
        outsiderJson!["Error"]!.GetValue<string>().Should().Be("Só as partes da troca podem avaliar.");

        // Rating fora de 1–5 → 400 de domínio (validado no aggregate).
        var invalid = await AuthedClient(buyer).PostAsync(
            $"/api/trades/{trade.Id}/reviews", JsonBody(new { Rating = 9, Comment = (string?)"exagero" }));
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var invalidJson = await invalid.Content.ReadFromJsonAsync<JsonNode>();
        invalidJson!["Error"]!.GetValue<string>().Should().Be("Avaliação deve estar entre 1 e 5.");

        // Perfil público do avaliado (buyer) lista a avaliação — leitura anônima, shape PascalCase.
        var list = await Http.GetAsync($"/api/users/{buyer.UserId}/reviews");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var arr = await list.Content.ReadFromJsonAsync<JsonArray>();
        var review = arr!.Single(r => r!["Id"]!.GetValue<Guid>() == reviewId)!;
        review["TradeId"]!.GetValue<Guid>().Should().Be(trade.Id);
        review["ReviewerId"]!.GetValue<Guid>().Should().Be(seller.UserId);
        review["Rating"]!.GetValue<int>().Should().Be(5);
        review["Comment"]!.GetValue<string>().Should().Be("Comprador pontual");

        // Fecha o loop UF-23: o evento da avaliação alimentou a reputação do avaliado.
        var reputation = await Http.GetAsync($"/api/users/{buyer.UserId}/reputation");
        reputation.StatusCode.Should().Be(HttpStatusCode.OK);
        var rep = await reputation.Content.ReadFromJsonAsync<JsonNode>();
        rep!["ReviewsCount"]!.GetValue<int>().Should().Be(1);
        rep["AvgRating"]!.GetValue<double>().Should().Be(5);
    }
}
