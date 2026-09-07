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

    [Fact]
    public async Task User_and_community_management_require_admin()
    {
        var user = await CreateUserAsync();
        var communityId = await CreateCommunityAsync(user);

        // GET de listas e ações de moderação: tudo 403 para não-admin.
        var getUsers = await AuthedClient(user).GetAsync("/api/admin/users");
        getUsers.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var getCommunities = await AuthedClient(user).GetAsync("/api/admin/communities");
        getCommunities.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var ban = await AuthedClient(user).PostAsync($"/api/admin/users/{user.UserId}/ban", null);
        ban.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var archive = await AuthedClient(user).PostAsync($"/api/admin/communities/{communityId}/archive", null);
        archive.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Ban_and_unban_user_flow()
    {
        var admin = await CreateAdminAsync();
        var user = await CreateUserAsync();

        // Trava de pé em pé: o admin não pode se banir.
        var self = await AuthedClient(admin).PostAsync($"/api/admin/users/{admin.UserId}/ban", null);
        self.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Ban → 204; lista do painel mostra Banned.
        var ban = await AuthedClient(admin).PostAsync($"/api/admin/users/{user.UserId}/ban", null);
        ban.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterBan = await AuthedClient(admin).GetAsync("/api/admin/users");
        afterBan.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await afterBan.Content.ReadFromJsonAsync<JsonArray>();
        users!.First(u => u!["Id"]!.GetValue<Guid>() == user.UserId)!["Status"]!
            .GetValue<string>().Should().Be("Banned");

        // Unban → 204; volta a Active.
        var unban = await AuthedClient(admin).PostAsync($"/api/admin/users/{user.UserId}/unban", null);
        unban.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterUnban = await AuthedClient(admin).GetAsync("/api/admin/users");
        var users2 = await afterUnban.Content.ReadFromJsonAsync<JsonArray>();
        users2!.First(u => u!["Id"]!.GetValue<Guid>() == user.UserId)!["Status"]!
            .GetValue<string>().Should().Be("Active");
    }

    [Fact]
    public async Task Ban_unknown_user_returns_400_api_error()
    {
        var admin = await CreateAdminAsync();

        var resp = await AuthedClient(admin).PostAsync($"/api/admin/users/{Guid.NewGuid()}/ban", null);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["Error"]!.GetValue<string>().Should().Be("Usuário não encontrado.");
    }

    [Fact]
    public async Task Archive_and_reactivate_community_flow()
    {
        var admin = await CreateAdminAsync();
        var creator = await CreateUserAsync();
        var communityId = await CreateCommunityAsync(creator);

        // Archive → 204; painel mostra Archived e feed público não lista mais.
        var archive = await AuthedClient(admin).PostAsync($"/api/admin/communities/{communityId}/archive", null);
        archive.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterArchive = await AuthedClient(admin).GetAsync("/api/admin/communities");
        afterArchive.StatusCode.Should().Be(HttpStatusCode.OK);
        var communities = await afterArchive.Content.ReadFromJsonAsync<JsonArray>();
        communities!.First(c => c!["Id"]!.GetValue<Guid>() == communityId)!["Status"]!
            .GetValue<string>().Should().Be("Archived");

        var feed = await AuthedClient(creator).GetAsync("/api/communities");
        var feedArr = await feed.Content.ReadFromJsonAsync<JsonArray>();
        feedArr!.Select(c => c!["Id"]!.GetValue<Guid>())
            .Should().NotContain(communityId);

        // Reactivate → 204; volta a Active e reaparece no feed.
        var reactivate = await AuthedClient(admin).PostAsync($"/api/admin/communities/{communityId}/reactivate", null);
        reactivate.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterReactivation = await AuthedClient(admin).GetAsync("/api/admin/communities");
        var communities2 = await afterReactivation.Content.ReadFromJsonAsync<JsonArray>();
        communities2!.First(c => c!["Id"]!.GetValue<Guid>() == communityId)!["Status"]!
            .GetValue<string>().Should().Be("Active");

        var feed2 = await AuthedClient(creator).GetAsync("/api/communities");
        var feedArr2 = await feed2.Content.ReadFromJsonAsync<JsonArray>();
        feedArr2!.Select(c => c!["Id"]!.GetValue<Guid>())
            .Should().Contain(communityId);
    }

    [Fact]
    public async Task Archive_unknown_community_returns_400_api_error()
    {
        var admin = await CreateAdminAsync();

        var resp = await AuthedClient(admin).PostAsync($"/api/admin/communities/{Guid.NewGuid()}/archive", null);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["Error"]!.GetValue<string>().Should().Be("Comunidade não encontrada.");
    }
}
