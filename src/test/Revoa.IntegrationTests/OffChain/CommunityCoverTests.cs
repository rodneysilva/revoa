using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Revoa.Community.Domain.Aggregates.MembershipAggregate;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Capa da comunidade (off-chain): upload real de mídia é do MediaTests; aqui
// validamos o comando de capa — gate Criador/Moderador, persistência no
// detalhe, remoção com null e capa enviada na criação.
public class CommunityCoverTests : IntegrationTestBase
{
    public CommunityCoverTests(ApiFactory factory) : base(factory) { }

    // Promove direto no Mongo — não há endpoint REST de promoção (só seeder/admin).
    private static async Task SeedModeratorAsync(ApiFactory factory, Guid userId, Guid communityId)
    {
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        var membership = Membership.Create(userId, "Moderador Seed", null, communityId, MembershipRole.Moderator);
        await database.GetCollection<Membership>("Memberships").InsertOneAsync(membership);
    }

    [Fact]
    public async Task Creator_sets_cover_and_detail_returns_it()
    {
        var creator = await CreateUserAsync();
        var client = AuthedClient(creator);
        var communityId = await CreateCommunityAsync(creator);

        var set = await client.PostAsync($"/api/communities/{communityId}/cover",
            JsonBody(new { CoverImageUrl = "/api/media/communities/capa.png" }));
        set.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await (await Http.GetAsync($"/api/communities/{communityId}"))
            .Content.ReadFromJsonAsync<JsonNode>();
        detail!["CoverImageUrl"]!.GetValue<string>().Should().Be("/api/media/communities/capa.png");

        // Capa null remove — volta ao gradiente do FE.
        var remove = await client.PostAsync($"/api/communities/{communityId}/cover",
            JsonBody(new { CoverImageUrl = (string?)null }));
        remove.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after = await (await Http.GetAsync($"/api/communities/{communityId}"))
            .Content.ReadFromJsonAsync<JsonNode>();
        (after!["CoverImageUrl"] as JsonNode).Should().BeNull();
    }

    [Fact]
    public async Task Moderator_can_set_cover_but_plain_member_cannot()
    {
        var creator = await CreateUserAsync();
        var communityId = await CreateCommunityAsync(creator);

        // Membro comum entra pela porta da frente (join).
        var member = await CreateUserAsync();
        var memberClient = AuthedClient(member);
        (await memberClient.PostAsync($"/api/communities/{communityId}/join", null)).StatusCode
            .Should().Be(HttpStatusCode.OK);

        var denied = await memberClient.PostAsync($"/api/communities/{communityId}/cover",
            JsonBody(new { CoverImageUrl = "/api/media/communities/hack.png" }));
        denied.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await denied.Content.ReadFromJsonAsync<JsonNode>())!["Error"]!.GetValue<string>()
            .Should().Be("Apenas moderadores ou o criador podem alterar a capa.");

        // Moderador (seed direto) pode.
        var moderator = await CreateUserAsync();
        await SeedModeratorAsync(Factory, moderator.UserId, communityId);
        var allowed = await AuthedClient(moderator).PostAsync($"/api/communities/{communityId}/cover",
            JsonBody(new { CoverImageUrl = "/api/media/communities/mod.png" }));
        allowed.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Cover_requires_auth()
    {
        var creator = await CreateUserAsync();
        var communityId = await CreateCommunityAsync(creator);

        var resp = await Http.PostAsync($"/api/communities/{communityId}/cover",
            JsonBody(new { CoverImageUrl = "/api/media/communities/x.png" }));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_community_with_cover_persists()
    {
        var creator = await CreateUserAsync();
        var create = await AuthedClient(creator).PostAsync("/api/communities", JsonBody(new
        {
            Name = "Comunidade com capa",
            Description = "Capa enviada na criação",
            Type = "User",
            Axis = "Interest",
            Visibility = "Open",
            CoverImageUrl = "/api/media/communities/nascimento.png",
        }));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var communityId = await ReadIdAsync(create);

        var detail = await (await Http.GetAsync($"/api/communities/{communityId}"))
            .Content.ReadFromJsonAsync<JsonNode>();
        detail!["CoverImageUrl"]!.GetValue<string>()
            .Should().Be("/api/media/communities/nascimento.png");
    }
}
