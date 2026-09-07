using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Feed público de posts (GET /api/posts, off-chain): raízes Visible de
// comunidades Active, mais recentes primeiro, 20 por página, anônimo vê.
public class PublicPostsTests : IntegrationTestBase
{
    public PublicPostsTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Public_feed_lists_roots_with_community_name_anonymously()
    {
        var creator = await CreateUserAsync();
        var c1 = await CreateCommunityAsync(creator);
        var c2 = await CreateCommunityAsync(creator);
        var p1 = await CreateCommunityPostAsync(creator, c1, "Post da comunidade 1");
        await CreateCommunityPostAsync(creator, c2, "Post da comunidade 2");

        // Resposta (ParentId) não é raiz — não pode aparecer no feed público.
        var reply = await AuthedClient(creator).PostAsync(
            $"/api/communities/{c1}/posts", JsonBody(new { ParentId = (Guid?)p1, Content = "Resposta" }));
        reply.StatusCode.Should().Be(HttpStatusCode.OK);

        var feed = await Http.GetAsync("/api/posts");
        feed.StatusCode.Should().Be(HttpStatusCode.OK);
        var arr = await feed.Content.ReadFromJsonAsync<JsonArray>();

        var contents = arr!.Select(p => p!["Post"]!["Content"]!.GetValue<string>()).ToList();
        contents.Should().Contain("Post da comunidade 1");
        contents.Should().Contain("Post da comunidade 2");
        contents.Should().NotContain("Resposta", "respostas não são raízes");

        var item = arr.Single(p => p!["Post"]!["Id"]!.GetValue<Guid>() == p1)!;
        item["CommunityName"]!.GetValue<string>().Should()
            .StartWith("Comunidade E2E", "o card do feed mostra o nome da comunidade");
    }

    [Fact]
    public async Task Archived_community_is_excluded_from_public_feed()
    {
        var creator = await CreateUserAsync();
        var keepId = await CreateCommunityAsync(creator);
        var dropId = await CreateCommunityAsync(creator);
        await CreateCommunityPostAsync(creator, keepId, "Post que fica");
        await CreateCommunityPostAsync(creator, dropId, "Post que sai");

        // Admin arquiva a segunda comunidade (fluxo real do painel).
        var admin = await CreateAdminAsync();
        var archive = await AuthedClient(admin)
            .PostAsync($"/api/admin/communities/{dropId}/archive", null);
        archive.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var arr = await (await Http.GetAsync("/api/posts")).Content.ReadFromJsonAsync<JsonArray>();
        var contents = arr!.Select(p => p!["Post"]!["Content"]!.GetValue<string>()).ToList();
        contents.Should().Contain("Post que fica");
        contents.Should().NotContain("Post que sai");
    }

    [Fact]
    public async Task Page_2_returns_next_batch_of_20()
    {
        var creator = await CreateUserAsync();
        var communityId = await CreateCommunityAsync(creator);

        // 22 raízes — página 1 enche 20, página 2 traz as 2 que sobraram.
        for (var i = 1; i <= 22; i++)
        {
            await CreateCommunityPostAsync(creator, communityId, $"Post paginado {i:00}");
        }

        var page1 = await (await Http.GetAsync("/api/posts?page=1"))
            .Content.ReadFromJsonAsync<JsonArray>();
        var page2 = await (await Http.GetAsync("/api/posts?page=2"))
            .Content.ReadFromJsonAsync<JsonArray>();

        page1!.Should().HaveCount(20);
        page2!.Should().HaveCount(2);

        var ids1 = page1.Select(p => p!["Post"]!["Id"]!.GetValue<Guid>()).ToList();
        var ids2 = page2.Select(p => p!["Post"]!["Id"]!.GetValue<Guid>()).ToList();
        ids1.Concat(ids2).Distinct().Should().HaveCount(22, "páginas não podem sobrepor");
    }

    [Fact]
    public async Task Viewer_token_enriches_IsLiked_in_public_feed()
    {
        var creator = await CreateUserAsync();
        var communityId = await CreateCommunityAsync(creator);
        var postId = await CreateCommunityPostAsync(creator, communityId, "Post do feed público");

        var liker = AuthedClient(await CreateUserAsync());
        await liker.PostAsync($"/api/posts/{postId}/like", null);

        var authed = await (await liker.GetAsync("/api/posts")).Content.ReadFromJsonAsync<JsonArray>();
        var authedPost = authed!.Single(p => p!["Post"]!["Id"]!.GetValue<Guid>() == postId)!;
        authedPost["Post"]!["IsLiked"]!.GetValue<bool>().Should().BeTrue();
        authedPost["Post"]!["LikeCount"]!.GetValue<int>().Should().Be(1);

        var anon = await (await Http.GetAsync("/api/posts")).Content.ReadFromJsonAsync<JsonArray>();
        var anonPost = anon!.Single(p => p!["Post"]!["Id"]!.GetValue<Guid>() == postId)!;
        anonPost["Post"]!["IsLiked"]!.GetValue<bool>().Should().BeFalse();
        anonPost["Post"]!["LikeCount"]!.GetValue<int>().Should().Be(1);
    }
}
