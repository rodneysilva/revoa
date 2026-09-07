using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Comunidades (off-chain): lacunas além de CommunityTests (criação/feed/posts/membros/senha).
// Aqui fechamos: 404 de detalhe, 401 das ações, 400 de parse de enum, saída de comunidade
// (membro sai / criador não sai) e ocultação de post (criador oculta; membro comum não oculta).
public class CommunityModerationTests : IntegrationTestBase
{
    public CommunityModerationTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Detail_unknown_community_returns_404_with_api_error()
    {
        var resp = await Http.GetAsync($"/api/communities/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["Error"]!.GetValue<string>().Should().Be("Comunidade não encontrada.");
    }

    [Fact]
    public async Task Member_actions_require_authentication()
    {
        var id = Guid.NewGuid();

        var create = await Http.PostAsync("/api/communities", JsonBody(new
        {
            Name = "Anônimo não cria",
            Description = "descrição",
            Type = "User",
            Axis = "Interest",
            Visibility = "Open",
            Password = (string?)null,
            Lat = (double?)null,
            Lng = (double?)null,
            Neighborhood = (string?)null,
            City = (string?)null,
            State = (string?)null,
        }));
        create.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var join = await Http.PostAsync($"/api/communities/{id}/join", JsonBody(new { Password = (string?)null }));
        join.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var leave = await Http.PostAsync($"/api/communities/{id}/leave", content: null);
        leave.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var post = await Http.PostAsync($"/api/communities/{id}/posts", JsonBody(new
        {
            ParentId = (Guid?)null,
            Content = "anônimo não posta",
        }));
        post.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_community_invalid_axis_returns_400_api_error()
    {
        var user = await CreateUserAsync();

        var resp = await AuthedClient(user).PostAsync("/api/communities", JsonBody(new
        {
            Name = "Eixo inválido E2E",
            Description = "descrição",
            Type = "User",
            Axis = "Filosofia", // parse de enum no controller → 400 antes do handler
            Visibility = "Open",
            Password = (string?)null,
            Lat = (double?)null,
            Lng = (double?)null,
            Neighborhood = (string?)null,
            City = (string?)null,
            State = (string?)null,
        }));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["Error"]!.GetValue<string>().Should().Be("Eixo inválido (Geo|Interest|Cause).");
    }

    [Fact]
    public async Task Member_can_leave_but_creator_cannot()
    {
        var creator = await CreateUserAsync();
        var communityId = await CreateCommunityAsync(creator);

        // Não-membro tenta sair → 400.
        var outsider = await CreateUserAsync();
        var notMember = await AuthedClient(outsider).PostAsync(
            $"/api/communities/{communityId}/leave", content: null);
        notMember.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var notMemberJson = await notMember.Content.ReadFromJsonAsync<JsonNode>();
        notMemberJson!["Error"]!.GetValue<string>().Should().Be("Você não é membro desta comunidade.");

        // Membro entra e sai → 204 sem corpo.
        var member = await CreateUserAsync();
        var memberClient = AuthedClient(member);
        var join = await memberClient.PostAsync(
            $"/api/communities/{communityId}/join", JsonBody(new { Password = (string?)null }));
        join.StatusCode.Should().Be(HttpStatusCode.OK);

        var leave = await memberClient.PostAsync($"/api/communities/{communityId}/leave", content: null);
        leave.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Criador não sai (deve arquivar a comunidade) → 400.
        var creatorLeave = await AuthedClient(creator).PostAsync(
            $"/api/communities/{communityId}/leave", content: null);
        creatorLeave.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var creatorJson = await creatorLeave.Content.ReadFromJsonAsync<JsonNode>();
        creatorJson!["Error"]!.GetValue<string>()
            .Should().Be("Criador deve arquivar a comunidade em vez de sair.");
    }

    [Fact]
    public async Task Creator_hides_post_but_common_member_cannot()
    {
        var creator = await CreateUserAsync();
        var communityId = await CreateCommunityAsync(creator);

        // Criador posta (é membro com papel Criador).
        var creatorClient = AuthedClient(creator);
        var post = await creatorClient.PostAsync($"/api/communities/{communityId}/posts", JsonBody(new
        {
            ParentId = (Guid?)null,
            Content = "Post que será oculto E2E",
        }));
        post.StatusCode.Should().Be(HttpStatusCode.OK);
        var postId = await ReadIdAsync(post);

        // Membro comum entra, tenta ocultar → 400 (gate Moderador/Criador no handler).
        var member = await CreateUserAsync();
        var memberClient = AuthedClient(member);
        var join = await memberClient.PostAsync(
            $"/api/communities/{communityId}/join", JsonBody(new { Password = (string?)null }));
        join.StatusCode.Should().Be(HttpStatusCode.OK);

        var byMember = await memberClient.PostAsync(
            $"/api/communities/{communityId}/posts/{postId}/hide", content: null);
        byMember.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var memberJson = await byMember.Content.ReadFromJsonAsync<JsonNode>();
        memberJson!["Error"]!.GetValue<string>()
            .Should().Be("Apenas moderadores ou o criador podem ocultar posts.");

        // Criador oculta → 204; o post sai do feed público.
        var hide = await creatorClient.PostAsync(
            $"/api/communities/{communityId}/posts/{postId}/hide", content: null);
        hide.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var posts = await Http.GetAsync($"/api/communities/{communityId}/posts");
        posts.StatusCode.Should().Be(HttpStatusCode.OK);
        var arr = await posts.Content.ReadFromJsonAsync<JsonArray>();
        arr!.Select(p => p!["Id"]!.GetValue<Guid>()).Should().NotContain(postId);
    }
}
