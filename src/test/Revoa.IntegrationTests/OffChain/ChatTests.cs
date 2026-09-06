using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Revoa.Community.Domain.Aggregates.ChatMessageAggregate;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Histórico do chat ao vivo (off-chain). O envio em si é SignalR (hub); aqui
// testamos o REST que o LiveChat consome ao abrir: leitura pública, ordem
// cronológica e limit. Mensagens são semeadas direto no Mongo (padrão OffChainSeed).
public class ChatTests : IntegrationTestBase
{
    public ChatTests(ApiFactory factory) : base(factory) { }

    private static async Task InsertChatAsync(ApiFactory factory, Guid communityId, Guid autorId, string conteudo)
    {
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        var message = ChatMessage.Create(communityId, autorId, "Autor Seed", null, conteudo);
        await database.GetCollection<ChatMessage>("ChatMessages").InsertOneAsync(message);
    }

    [Fact]
    public async Task Chat_history_is_public_and_chronological()
    {
        var creator = await CreateUserAsync();
        var client = AuthedClient(creator);

        var create = await client.PostAsync("/api/communities", JsonBody(new
        {
            Name = "Chat E2E",
            Description = "Comunidade p/ histórico",
            Type = "User",
            Axis = "Interest",
            Visibility = "Open",
        }));
        var communityId = await ReadIdAsync(create);

        // Duas mensagens: a mais antiga entra primeiro no Mongo.
        await InsertChatAsync(Factory, communityId, creator.UserId, "primeira (antiga)");
        await InsertChatAsync(Factory, communityId, creator.UserId, "segunda (recente)");

        // Leitura anônima (sem Bearer) — histórico é público como os posts.
        var resp = await Http.GetAsync($"/api/communities/{communityId}/chat");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await resp.Content.ReadFromJsonAsync<JsonArray>();
        json.Should().NotBeNull();
        json!.Count.Should().Be(2);
        json[0]!["Content"]!.GetValue<string>().Should().Be("primeira (antiga)",
            "a UI espera ordem cronológica (mais antigas primeiro)");
        json[1]!["Content"]!.GetValue<string>().Should().Be("segunda (recente)");
        json[0]!["AuthorName"]!.GetValue<string>().Should().Be("Autor Seed");
    }

    [Fact]
    public async Task Chat_history_unknown_community_returns_404()
    {
        var resp = await Http.GetAsync($"/api/communities/{Guid.NewGuid()}/chat");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
