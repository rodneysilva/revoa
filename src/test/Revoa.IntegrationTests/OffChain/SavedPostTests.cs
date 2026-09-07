using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Salvar posts (off-chain): bookmark privado por usuário (cap 200), toggle
// idempotente, listagem em cards com IsSaved=true e bootstrap de ids.
public class SavedPostTests : IntegrationTestBase
{
    public SavedPostTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Save_requires_auth()
    {
        var creator = await CreateUserAsync();
        var communityId = await CreateCommunityAsync(creator);
        var postId = await CreateCommunityPostAsync(creator, communityId, "Post alvo");

        var save = await Http.PostAsync($"/api/posts/{postId}/save", null);
        save.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var list = await Http.GetAsync("/api/posts/saved");
        list.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var ids = await Http.GetAsync("/api/posts/saved/ids");
        ids.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Save_lists_cards_and_ids()
    {
        var creator = await CreateUserAsync();
        var communityId = await CreateCommunityAsync(creator);
        var postId = await CreateCommunityPostAsync(creator, communityId, "Post salvável");

        var user = AuthedClient(await CreateUserAsync());

        var on = await user.PostAsync($"/api/posts/{postId}/save", null);
        on.StatusCode.Should().Be(HttpStatusCode.OK);
        (await on.Content.ReadFromJsonAsync<JsonNode>())!["Saved"]!.GetValue<bool>()
            .Should().BeTrue();

        // Cards: bookmark vira PostDto com IsSaved=true e Content do post.
        var cards = await (await user.GetAsync("/api/posts/saved"))
            .Content.ReadFromJsonAsync<JsonArray>();
        cards!.Should().HaveCount(1);
        cards[0]!["Id"]!.GetValue<Guid>().Should().Be(postId);
        cards[0]!["Content"]!.GetValue<string>().Should().Be("Post salvável");
        cards[0]!["IsSaved"]!.GetValue<bool>().Should().BeTrue();

        // Bootstrap de ids.
        var ids = await (await user.GetAsync("/api/posts/saved/ids"))
            .Content.ReadFromJsonAsync<JsonArray>();
        ids!.Select(i => i!.GetValue<Guid>()).Should().Contain(postId);
    }

    [Fact]
    public async Task Unsave_removes_from_saved_list()
    {
        var creator = await CreateUserAsync();
        var communityId = await CreateCommunityAsync(creator);
        var postId = await CreateCommunityPostAsync(creator, communityId, "Post temporário");

        var user = AuthedClient(await CreateUserAsync());
        await user.PostAsync($"/api/posts/{postId}/save", null);

        var off = await user.PostAsync($"/api/posts/{postId}/save", null);
        (await off.Content.ReadFromJsonAsync<JsonNode>())!["Saved"]!.GetValue<bool>()
            .Should().BeFalse();

        var cards = await (await user.GetAsync("/api/posts/saved"))
            .Content.ReadFromJsonAsync<JsonArray>();
        cards!.Should().BeEmpty();
    }

    [Fact]
    public async Task Save_unknown_post_returns_404()
    {
        var user = AuthedClient(await CreateUserAsync());
        var resp = await user.PostAsync($"/api/posts/{Guid.NewGuid()}/save", null);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Saved_posts_are_private_per_user()
    {
        var creator = await CreateUserAsync();
        var communityId = await CreateCommunityAsync(creator);
        var postId = await CreateCommunityPostAsync(creator, communityId, "Post do A");

        var a = AuthedClient(await CreateUserAsync());
        await a.PostAsync($"/api/posts/{postId}/save", null);

        var b = AuthedClient(await CreateUserAsync());
        var cards = await (await b.GetAsync("/api/posts/saved"))
            .Content.ReadFromJsonAsync<JsonArray>();
        cards!.Should().BeEmpty("salvos são privados por usuário");

        var ids = await (await b.GetAsync("/api/posts/saved/ids"))
            .Content.ReadFromJsonAsync<JsonArray>();
        ids!.Should().BeEmpty();
    }
}
