using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Autorização do resolve de disputas (move fundos do escrow): policy "Arbitrator" — role
// ARBITRATOR via claim "role" no JWT, ou Admin (allowlist Admin:Emails). Usuário comum → 403.
public class ResolveAuthzTests : IntegrationTestBase
{
    public ResolveAuthzTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Resolve_returns_403_for_regular_verified_user()
    {
        var user = await CreateUserAsync();
        var client = AuthedClient(user);

        var resp = await client.PostAsync(
            $"/api/trades/{Guid.NewGuid()}/resolve", JsonBody(new { ReleaseToSeller = true }));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Resolve_accepts_arbitrator_role_and_rejects_unknown_trade()
    {
        // Admin promove a Árbitro; novo login emite JWT com a claim role → passa a policy.
        var admin = await CreateAdminAsync();
        var adminClient = AuthedClient(admin);

        var email = $"arbitro-{Guid.NewGuid():N}@revoa.test";
        var userId = await RegisterAsync("Arbitro E2E", email);
        await DevVerifyAsync(email, "Arbitro E2E");

        var promote = await adminClient.PutAsync(
            $"/api/admin/users/{userId}/role", JsonBody(new { Role = "Arbitrator" }));
        promote.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Novo JWT (login dev) carrega a role: resolve passa a policy e falha só por trade inexistente.
        var token = await LoginAsync(email);
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.PostAsync(
            $"/api/trades/{Guid.NewGuid()}/resolve", JsonBody(new { ReleaseToSeller = true }));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetUserRole_requires_admin()
    {
        var user = await CreateUserAsync();
        var client = AuthedClient(user);

        var resp = await client.PutAsync(
            $"/api/admin/users/{user.UserId}/role", JsonBody(new { Role = "Arbitrator" }));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
