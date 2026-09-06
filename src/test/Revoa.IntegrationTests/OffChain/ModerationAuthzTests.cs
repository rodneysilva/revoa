using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Moderação (off-chain): gates e validações de entrada. O fluxo feliz denúncia→lista→resolução
// está em ModerationTests; aqui fechamos 401/403 e os 400 de parse de enum/uuid (validados no
// controller, antes de qualquer handler).
public class ModerationAuthzTests : IntegrationTestBase
{
    public ModerationAuthzTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Create_report_requires_authentication()
    {
        var resp = await Http.PostAsync("/api/reports", JsonBody(new
        {
            TargetType = "Listing",
            TargetId = Guid.NewGuid().ToString(),
            Reason = "Spam",
            Details = (string?)null,
        }));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_report_rejects_invalid_target_type()
    {
        var user = await CreateUserAsync();

        var resp = await AuthedClient(user).PostAsync("/api/reports", JsonBody(new
        {
            TargetType = "Planet", // não é Listing|Post|User|Comment
            TargetId = Guid.NewGuid().ToString(),
            Reason = "Spam",
            Details = (string?)null,
        }));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["Error"]!.GetValue<string>()
            .Should().Be("Tipo de alvo ou motivo de denúncia inválido.");
    }

    [Fact]
    public async Task Create_report_rejects_invalid_target_id()
    {
        var user = await CreateUserAsync();

        var resp = await AuthedClient(user).PostAsync("/api/reports", JsonBody(new
        {
            TargetType = "Listing",
            TargetId = "nao-e-um-guid",
            Reason = "Spam",
            Details = (string?)null,
        }));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["Error"]!.GetValue<string>().Should().Be("Alvo da denúncia inválido.");
    }

    [Fact]
    public async Task List_reports_requires_admin()
    {
        var anonymous = await Http.GetAsync("/api/reports");
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var user = await CreateUserAsync();
        var byUser = await AuthedClient(user).GetAsync("/api/reports");
        byUser.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Resolve_report_rejects_invalid_action()
    {
        var admin = await CreateAdminAsync();

        var resp = await AuthedClient(admin).PostAsync(
            $"/api/reports/{Guid.NewGuid()}/resolve",
            JsonBody(new { Action = "Explodir", Note = (string?)null }));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["Error"]!.GetValue<string>().Should().Be("Ação de resolução inválida.");
    }

    [Fact]
    public async Task Resolve_unknown_report_returns_400_api_error()
    {
        var admin = await CreateAdminAsync();

        var resp = await AuthedClient(admin).PostAsync(
            $"/api/reports/{Guid.NewGuid()}/resolve",
            JsonBody(new { Action = "Dismissed", Note = (string?)"E2E" }));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["Error"]!.GetValue<string>().Should().Be("Denúncia não encontrada.");
    }
}
