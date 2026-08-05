using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OnChain;

// Cupom on-chain (UF-29): admin cria cupom → usuário verificado resgata (mint on-chain de RVM).
// ON-CHAIN: pula graceful se o anvil + contratos não estiverem acessíveis.
public class CouponTests : IntegrationTestBase
{
    public CouponTests(ApiFactory factory) : base(factory) { }

    [SkippableFact]
    public async Task Admin_creates_coupon_and_user_redeems()
    {
        SkipIfChainUnavailable();

        // Admin cria cupom.
        var admin = await CreateAdminAsync();
        var adminClient = AuthedClient(admin);
        var create = await adminClient.PostAsync("/api/coupons", JsonBody(new
        {
            AmountRvm = 10L,
            MaxUses = 5,
            Expiry = (string?)null,
            Code = (string?)null,
        }));
        create.StatusCode.Should().Be(HttpStatusCode.OK);
        var coupon = await create.Content.ReadFromJsonAsync<JsonNode>();
        var code = coupon!["Code"]!.GetValue<string>();
        code.Should().NotBeNullOrWhiteSpace();

        // Usuário verificado resgata (mint on-chain).
        var user = await CreateUserAsync();
        var redeem = await AuthedClient(user).PostAsync("/api/coupons/redeem", JsonBody(new
        {
            Code = code,
        }));
        redeem.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
