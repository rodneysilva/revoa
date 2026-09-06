using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Cupons (off-chain, sempre verde). Criar/revogar/resgatar de verdade movem a chain (mint RVM /
// CouponRedeemer) e o happy path é coberto em OnChain/CouponTests; aqui cobrimos os gates
// Admin/Verified (401/403 avaliados antes do handler) e as validações 400 que antecedem a chain.
public class CouponsAdminTests : IntegrationTestBase
{
    public CouponsAdminTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Create_requires_admin()
    {
        var body = JsonBody(new { AmountRvm = 10L, MaxUses = 5, Expiry = (string?)null, Code = (string?)null });

        var anonymous = await Http.PostAsync("/api/coupons", body);
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var user = await CreateUserAsync();
        var byUser = await AuthedClient(user).PostAsync("/api/coupons", JsonBody(new
        {
            AmountRvm = 10L,
            MaxUses = 5,
            Expiry = (string?)null,
            Code = (string?)null,
        }));
        byUser.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task List_returns_array_for_admin_and_403_for_common_user()
    {
        var user = await CreateUserAsync();

        var anonymous = await Http.GetAsync("/api/coupons");
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var byUser = await AuthedClient(user).GetAsync("/api/coupons");
        byUser.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Admin vê a lista (vazia — o reset limpa a coleção; criação real exige chain).
        var admin = await CreateAdminAsync();
        var byAdmin = await AuthedClient(admin).GetAsync("/api/coupons");
        byAdmin.StatusCode.Should().Be(HttpStatusCode.OK);
        var arr = await byAdmin.Content.ReadFromJsonAsync<JsonArray>();
        arr.Should().NotBeNull();
    }

    [Fact]
    public async Task Revoke_unknown_coupon_returns_400_and_requires_admin()
    {
        var user = await CreateUserAsync();

        var byUser = await AuthedClient(user).PostAsync($"/api/coupons/{Guid.NewGuid()}/revoke", content: null);
        byUser.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Cupom inexistente falha ANTES da chain (leitura no Mongo).
        var admin = await CreateAdminAsync();
        var resp = await AuthedClient(admin).PostAsync($"/api/coupons/{Guid.NewGuid()}/revoke", content: null);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["Error"]!.GetValue<string>().Should().Be("Cupom não encontrado.");
    }

    [Fact]
    public async Task Redeem_requires_auth_and_non_empty_code()
    {
        var anonymous = await Http.PostAsync("/api/coupons/redeem", JsonBody(new { Code = "ABCDEFGH" }));
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Código vazio/nulo falha ANTES da chain (validação do handler).
        var user = await CreateUserAsync();
        var resp = await AuthedClient(user).PostAsync("/api/coupons/redeem", JsonBody(new
        {
            Code = (string?)null,
        }));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["Error"]!.GetValue<string>().Should().Be("Informe o código do cupom.");
    }
}
