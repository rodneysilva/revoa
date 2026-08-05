using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Fluxo de autenticação (off-chain, sempre verde). Cobre registro, ativação dev-verify, login dev
// e a falha de login com e-mail inexistente.
public class AuthTests : IntegrationTestBase
{
    public AuthTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Register_returns_user_id()
    {
        var email = $"auth-{Guid.NewGuid():N}@revoa.test";

        var resp = await Http.PostAsync("/api/auth/register", JsonBody(new
        {
            Nome = "Usuario Auth E2E",
            Email = email,
            Telefone = "+5511980001111",
            BirthDate = "1990-01-01",
            CouponCode = (string?)null,
        }));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["UserId"]!.GetValue<Guid>().Should().NotBeEmpty();
        json["NeedsEmailVerification"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task DevVerify_activates_and_returns_token()
    {
        var email = $"verify-{Guid.NewGuid():N}@revoa.test";
        await RegisterAsync("Usuario Verify E2E", email);

        var token = await DevVerifyAsync(email, "Usuario Verify E2E");

        token.Token.Should().NotBeNullOrWhiteSpace();
        token.UserId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Login_returns_token_for_verified_user()
    {
        var email = $"login-{Guid.NewGuid():N}@revoa.test";
        await RegisterAsync("Usuario Login E2E", email);
        await DevVerifyAsync(email, "Usuario Login E2E");

        var token = await LoginAsync(email);

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_with_unknown_email_returns_400()
    {
        var resp = await Http.PostAsync("/api/auth/login", JsonBody(new
        {
            Email = $"inexistente-{Guid.NewGuid():N}@revoa.test",
        }));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
