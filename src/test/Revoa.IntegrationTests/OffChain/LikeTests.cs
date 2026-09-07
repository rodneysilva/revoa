using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Curtir posts (off-chain): toggle idempotente por (post, usuário), contador
// público no GET de posts, IsLiked só com JWT. Curtir NÃO exige membership —
// o feed público mostra posts de comunidades que o usuário não participa.
public class LikeTests : IntegrationTestBase
{
    public LikeTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Like_requires_auth()
    {
        var creator = await CreateUserAsync();
        var communityId = await CreateCommunityAsync(creator);
        var postId = await CreateCommunityPostAsync(creator, communityId, "Post alvo");

        var like = await Http.PostAsync($"/api/posts/{postId}/like", null);
        like.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var ids = await Http.GetAsync("/api/posts/liked/ids");
        ids.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Toggle_like_updates_count_and_IsLiked_in_community_posts()
    {
        var creator = await CreateUserAsync();
        var communityId = await CreateCommunityAsync(creator);
        var postId = await CreateCommunityPostAsync(creator, communityId, "Post curtível");

        // Liker nunca entrou na comunidade — curtir não tem gate de membership.
        var liker = await CreateUserAsync();
        var likerClient = AuthedClient(liker);

        var on = await likerClient.PostAsync($"/api/posts/{postId}/like", null);
        on.StatusCode.Should().Be(HttpStatusCode.OK);
        (await on.Content.ReadFromJsonAsync<JsonNode>())!["Liked"]!.GetValue<bool>()
            .Should().BeTrue();

        // Com token: IsLiked true + contador público.
        var authed = await (await likerClient.GetAsync($"/api/communities/{communityId}/posts"))
            .Content.ReadFromJsonAsync<JsonArray>();
        var liked = authed!.Single(p => p!["Id"]!.GetValue<Guid>() == postId)!;
        liked["IsLiked"]!.GetValue<bool>().Should().BeTrue();
        liked["LikeCount"]!.GetValue<int>().Should().Be(1);

        // Anônimo: mesmo contador, IsLiked false.
        var anon = await (await Http.GetAsync($"/api/communities/{communityId}/posts"))
            .Content.ReadFromJsonAsync<JsonArray>();
        var anonPost = anon!.Single(p => p!["Id"]!.GetValue<Guid>() == postId)!;
        anonPost["IsLiked"]!.GetValue<bool>().Should().BeFalse();
        anonPost["LikeCount"]!.GetValue<int>().Should().Be(1);

        // Toggle de volta → descurtido, contador zero.
        var off = await likerClient.PostAsync($"/api/posts/{postId}/like", null);
        (await off.Content.ReadFromJsonAsync<JsonNode>())!["Liked"]!.GetValue<bool>()
            .Should().BeFalse();

        var after = await (await Http.GetAsync($"/api/communities/{communityId}/posts"))
            .Content.ReadFromJsonAsync<JsonArray>();
        after!.Single(p => p!["Id"]!.GetValue<Guid>() == postId)!["LikeCount"]!
            .GetValue<int>().Should().Be(0);
    }

    [Fact]
    public async Task Like_count_aggregates_distinct_users()
    {
        var creator = await CreateUserAsync();
        var communityId = await CreateCommunityAsync(creator);
        var postId = await CreateCommunityPostAsync(creator, communityId, "Post multi-like");

        var a = AuthedClient(await CreateUserAsync());
        var b = AuthedClient(await CreateUserAsync());
        (await a.PostAsync($"/api/posts/{postId}/like", null)).StatusCode
            .Should().Be(HttpStatusCode.OK);
        (await b.PostAsync($"/api/posts/{postId}/like", null)).StatusCode
            .Should().Be(HttpStatusCode.OK);

        var posts = await (await Http.GetAsync($"/api/communities/{communityId}/posts"))
            .Content.ReadFromJsonAsync<JsonArray>();
        posts!.Single(p => p!["Id"]!.GetValue<Guid>() == postId)!["LikeCount"]!
            .GetValue<int>().Should().Be(2);

        // Um descurte → sobra o like do outro.
        await a.PostAsync($"/api/posts/{postId}/like", null);
        var after = await (await Http.GetAsync($"/api/communities/{communityId}/posts"))
            .Content.ReadFromJsonAsync<JsonArray>();
        after!.Single(p => p!["Id"]!.GetValue<Guid>() == postId)!["LikeCount"]!
            .GetValue<int>().Should().Be(1);
    }

    [Fact]
    public async Task Like_unknown_post_returns_404()
    {
        var user = AuthedClient(await CreateUserAsync());
        var resp = await user.PostAsync($"/api/posts/{Guid.NewGuid()}/like", null);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Liked_ids_bootstrap_lists_all_likes()
    {
        var creator = await CreateUserAsync();
        var communityId = await CreateCommunityAsync(creator);
        var post1 = await CreateCommunityPostAsync(creator, communityId, "Post 1");
        var post2 = await CreateCommunityPostAsync(creator, communityId, "Post 2");

        var user = AuthedClient(await CreateUserAsync());
        await user.PostAsync($"/api/posts/{post1}/like", null);
        await user.PostAsync($"/api/posts/{post2}/like", null);

        var ids = await (await user.GetAsync("/api/posts/liked/ids"))
            .Content.ReadFromJsonAsync<JsonArray>();
        ids!.Select(i => i!.GetValue<Guid>())
            .Should().BeEquivalentTo([post1, post2]);
    }
}
