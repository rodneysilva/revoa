using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Feed social (GET /api/communities/mine/posts) — o rail "Da sua comunidade"
// do feed em uma chamada (antes: myCommunities + N× communityPosts no cliente).
// Aqui: mescla das comunidades do usuário em ordem desc, exclusão de comunidades
// não-membro, vazio sem vínculos e 401 anônimo.
public class SocialFeedTests : IntegrationTestBase
{
    public SocialFeedTests(ApiFactory factory) : base(factory) { }

    private static async Task<Guid> CreateCommunityAsync(HttpClient client, string name)
    {
        var resp = await client.PostAsync("/api/communities", JsonBody(new
        {
            Name = name,
            Description = "Comunidade p/ feed social",
            Type = "User",
            Axis = "Interest",
            Visibility = "Open",
        }));
        return await ReadIdAsync(resp);
    }

    [Fact]
    public async Task Social_feed_merges_my_communities_and_excludes_others()
    {
        var creator = await CreateUserAsync();
        var client = AuthedClient(creator);

        var communityA = await CreateCommunityAsync(client, "Feed Social A");
        var communityB = await CreateCommunityAsync(client, "Feed Social B");
        await Task.Delay(20); // CreatedAt com resolução distinguível entre posts.

        var postA = await client.PostAsync(
            $"/api/communities/{communityA}/posts", JsonBody(new { Content = "post da comunidade A" }));
        postA.StatusCode.Should().Be(HttpStatusCode.OK);
        await Task.Delay(20);
        var postB = await client.PostAsync(
            $"/api/communities/{communityB}/posts", JsonBody(new { Content = "post da comunidade B" }));
        postB.StatusCode.Should().Be(HttpStatusCode.OK);

        // Post em comunidade que o criador NÃO integra — deve ficar de fora.
        var outsider = await CreateUserAsync();
        var outsiderClient = AuthedClient(outsider);
        var communityC = await CreateCommunityAsync(outsiderClient, "Feed Social C");
        var postC = await outsiderClient.PostAsync(
            $"/api/communities/{communityC}/posts", JsonBody(new { Content = "post de fora" }));
        postC.StatusCode.Should().Be(HttpStatusCode.OK);

        var resp = await client.GetAsync("/api/communities/mine/posts");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await resp.Content.ReadFromJsonAsync<JsonArray>();
        json.Should().NotBeNull();
        json!.Count.Should().Be(2, "só posts das comunidades do usuário entram no feed social");
        json[0]!["Post"]!["Content"]!.GetValue<string>().Should().Be("post da comunidade B",
            "mais recente primeiro (desc)");
        json[0]!["Community"]!["Name"]!.GetValue<string>().Should().Be("Feed Social B",
            "o item carrega a comunidade para o rail linkar");
        json[1]!["Post"]!["Content"]!.GetValue<string>().Should().Be("post da comunidade A");
    }

    [Fact]
    public async Task Social_feed_without_memberships_returns_empty()
    {
        var user = await CreateUserAsync();
        var client = AuthedClient(user);

        var resp = await client.GetAsync("/api/communities/mine/posts");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await resp.Content.ReadFromJsonAsync<JsonArray>();
        json.Should().NotBeNull();
        json!.Count.Should().Be(0);
    }

    [Fact]
    public async Task Social_feed_requires_auth()
    {
        var resp = await Http.GetAsync("/api/communities/mine/posts");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
