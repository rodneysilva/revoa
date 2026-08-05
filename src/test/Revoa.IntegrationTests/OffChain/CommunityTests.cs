using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Comunidades (off-chain, sempre verde). 100% off-chain (posts/memberships não tocam a chain).
public class CommunityTests : IntegrationTestBase
{
    public CommunityTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Create_community_appears_in_feed_and_supports_posts_and_members()
    {
        var creator = await CreateUserAsync();
        var client = AuthedClient(creator);

        var create = await client.PostAsync("/api/communities", JsonBody(new
        {
            Nome = "Comunidade E2E",
            Descricao = "Descrição E2E",
            Tipo = "User",
            Eixo = "Interesse",
            Visibilidade = "Open",
            Password = (string?)null,
            Lat = (double?)null,
            Lng = (double?)null,
            Bairro = (string?)null,
            Cidade = (string?)null,
            Estado = (string?)null,
        }));

        create.IsSuccessStatusCode.Should().BeTrue(
            $"esperado 2xx ao criar comunidade: {await create.Content.ReadAsStringAsync()}");
        var communityId = Guid.Parse((await create.Content.ReadAsStringAsync()).Trim('"'));
        communityId.Should().NotBeEmpty();

        // GET /api/communities → contém a comunidade criada.
        var feed = await client.GetAsync("/api/communities");
        feed.StatusCode.Should().Be(HttpStatusCode.OK);
        var feedArr = await feed.Content.ReadFromJsonAsync<JsonArray>();
        feedArr!.Select(c => c!["Id"]!.GetValue<Guid>())
            .Should().Contain(communityId);

        // POST /api/communities/{id}/posts → criador é membro (papel Criador), pode postar.
        var postResp = await client.PostAsync($"/api/communities/{communityId}/posts", JsonBody(new
        {
            ParentId = (Guid?)null,
            Conteudo = "Post raiz E2E",
        }));
        postResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // GET /api/communities/{id}/posts → retorna o post criado.
        var posts = await client.GetAsync($"/api/communities/{communityId}/posts");
        posts.StatusCode.Should().Be(HttpStatusCode.OK);
        var postsArr = await posts.Content.ReadFromJsonAsync<JsonArray>();
        postsArr!.Select(p => p!["Conteudo"]!.GetValue<string>())
            .Should().Contain("Post raiz E2E");

        // GET /api/communities/{id}/members → criador está como membro.
        var members = await client.GetAsync($"/api/communities/{communityId}/members");
        members.StatusCode.Should().Be(HttpStatusCode.OK);
        var membersArr = await members.Content.ReadFromJsonAsync<JsonArray>();
        membersArr!.Select(m => m!["UsuarioId"]!.GetValue<Guid>())
            .Should().Contain(creator.UserId);
    }
}
