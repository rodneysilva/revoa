using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Admin (off-chain): role management + 403 de não-admin nos endpoints de parâmetros. O PUT de
// parâmetros feliz/400 já está em AdminParamsTests; a promoção p/ Árbitro (feed da policy do
// resolve) está em ResolveAuthzTests. Aqui fechamos: 403 de role em parameters, PUT role com
// usuário inexistente (400), role válida (Mod) e valor inválido de enum (400 de model binding).
public class AdminTests : IntegrationTestBase
{
    public AdminTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Parameters_require_admin()
    {
        var user = await CreateUserAsync();

        var get = await AuthedClient(user).GetAsync("/api/admin/parameters");
        get.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var put = await AuthedClient(user).PutAsync(
            "/api/admin/parameters/DonationReward.BonusRvm", JsonBody(new { Value = 7 }));
        put.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetUserRole_unknown_user_returns_400_api_error()
    {
        var admin = await CreateAdminAsync();

        var resp = await AuthedClient(admin).PutAsync(
            $"/api/admin/users/{Guid.NewGuid()}/role", JsonBody(new { Role = "Mod" }));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["Error"]!.GetValue<string>().Should().Be("Usuário não encontrado.");
    }

    [Fact]
    public async Task SetUserRole_promotes_to_mod()
    {
        var admin = await CreateAdminAsync();
        var user = await CreateUserAsync();

        var resp = await AuthedClient(admin).PutAsync(
            $"/api/admin/users/{user.UserId}/role", JsonBody(new { Role = "Mod" }));

        // Contrato: sucesso sem corpo = 204. A role alimenta a claim "role" do próximo JWT.
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SetUserRole_invalid_role_value_returns_400()
    {
        var admin = await CreateAdminAsync();
        var user = await CreateUserAsync();

        // Enum inválido → 400 de model binding ([ApiController]; sem envelope ApiError).
        var resp = await AuthedClient(admin).PutAsync(
            $"/api/admin/users/{user.UserId}/role", JsonBody(new { Role = "NaoExiste" }));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
