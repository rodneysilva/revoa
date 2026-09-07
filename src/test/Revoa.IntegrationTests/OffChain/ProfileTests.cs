using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Perfil público (api/users/{id} + atividade social). Privacidade em primeiro
// lugar: e-mail/telefone jamais saem; posts de comunidade Private não vazam.
public class ProfileTests : IntegrationTestBase
{
    public ProfileTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Public_profile_shows_name_but_never_contact_data()
    {
        var user = await CreateUserAsync(nome: "Maria Perfeco");

        var resp = await Http.GetAsync($"/api/users/{user.UserId}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["Name"]!.GetValue<string>().Should().Be("Maria Perfeco");
        json["MemberSince"].Should().NotBeNull("Created no registro é hoje");
        json["Verified"]!.GetValue<bool>().Should().BeTrue("dev-verify confirma e-mail e telefone → selo público");

        // Contrato de privacidade: nada de e-mail/telefone no payload público.
        var raw = await resp.Content.ReadAsStringAsync();
        raw.Should().NotContain(user.Email, "e-mail nunca pode vazar no perfil público");
        raw.Should().NotContain("Email").And.NotContain("Phone");
    }

    [Fact]
    public async Task Unknown_user_returns_404()
    {
        var resp = await Http.GetAsync($"/api/users/{Guid.NewGuid()}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Communities_lists_only_active_memberships()
    {
        var owner = await CreateUserAsync();
        var client = AuthedClient(owner);

        var create = await client.PostAsync("/api/communities", JsonBody(new
        {
            Name = "Comunidade do Perfil",
            Description = "Teste",
            Type = "User",
            Axis = "Interest",
            Visibility = "Open",
        }));
        var communityId = await ReadIdAsync(create);

        var resp = await Http.GetAsync($"/api/users/{owner.UserId}/communities");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var arr = await resp.Content.ReadFromJsonAsync<JsonArray>();
        arr.Should().HaveCount(1);
        arr![0]!["Name"]!.GetValue<string>().Should().Be("Comunidade do Perfil");
        arr[0]!["MembersCount"]!.GetValue<int>().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Posts_show_open_community_activity_but_not_private()
    {
        var author = await CreateUserAsync();
        var client = AuthedClient(author);

        // Comunidade Open + post → aparece.
        var open = await client.PostAsync("/api/communities", JsonBody(new
        {
            Name = "Aberta do Perfil",
            Description = "Teste",
            Type = "User",
            Axis = "Interest",
            Visibility = "Open",
        }));
        var openId = await ReadIdAsync(open);
        await client.PostAsync($"/api/communities/{openId}/posts", JsonBody(new { ParentId = (string?)null, Content = "post público do autor" }));

        // Comunidade Private (exige senha) + post → NÃO aparece no perfil público.
        var privada = await client.PostAsync("/api/communities", JsonBody(new
        {
            Name = "Privada do Perfil",
            Description = "Teste",
            Type = "User",
            Axis = "Interest",
            Visibility = "Private",
            Password = "senha-teste",
        }));
        var privadaId = await ReadIdAsync(privada);
        var postPrivado = await client.PostAsync($"/api/communities/{privadaId}/posts", JsonBody(new { ParentId = (string?)null, Content = "segredo da comunidade privada" }));
        postPrivado.IsSuccessStatusCode.Should().BeTrue(
            $"private community post should be created (got {(int)postPrivado.StatusCode}: {await postPrivado.Content.ReadAsStringAsync()})");

        var resp = await Http.GetAsync($"/api/users/{author.UserId}/posts");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var arr = await resp.Content.ReadFromJsonAsync<JsonArray>();
        arr.Should().HaveCount(1, "só o post da comunidade Open é atividade pública");
        arr![0]!["Post"]!["Content"]!.GetValue<string>().Should().Be("post público do autor");
        arr[0]!["Community"]!["Name"]!.GetValue<string>().Should().Be("Aberta do Perfil");
    }
}
