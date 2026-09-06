using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Notificações pessoais (off-chain, sempre verde). Dados pessoais → nada de leitura anônima;
// tudo exige Verified e o UserId vem sempre do token. A única via HTTP de criar notificação é a
// doação concluída (on-chain), então os testes seedam notificações direto no Mongo (OffChainSeed)
// para exercitar listar/marcar-read/contador — ownership é o ponto crítico aqui.
public class NotificationsTests : IntegrationTestBase
{
    public NotificationsTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task All_endpoints_require_authentication()
    {
        var id = Guid.NewGuid();

        var list = await Http.GetAsync("/api/notifications");
        list.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var count = await Http.GetAsync("/api/notifications/unread-count");
        count.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var read = await Http.PostAsync($"/api/notifications/{id}/read", content: null);
        read.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var subscribe = await Http.PostAsync("/api/notifications/push/subscribe", JsonBody(new
        {
            Endpoint = "https://push.example/e2e",
            P256dh = "chave",
            Auth = "segredo",
        }));
        subscribe.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var unsubscribe = await Http.DeleteAsync("/api/notifications/push/subscribe?endpoint=https://push.example/e2e");
        unsubscribe.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_returns_only_own_notifications_with_pascal_case_shape()
    {
        var user = await CreateUserAsync();
        var other = await CreateUserAsync();
        var seeded = await OffChainSeed.InsertNotificationsAsync(Factory, user.UserId, count: 2);
        await OffChainSeed.InsertNotificationsAsync(Factory, other.UserId, count: 3);

        var resp = await AuthedClient(user).GetAsync("/api/notifications");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var arr = await resp.Content.ReadFromJsonAsync<JsonArray>();
        arr!.Select(n => n!["Id"]!.GetValue<Guid>())
            .Should().BeEquivalentTo(seeded.Select(n => n.Id));

        // Shape PascalCase do DTO (Body/Read são os campos legados do aggregate).
        var item = arr.First(i => i!["Id"]!.GetValue<Guid>() == seeded[0].Id)!;
        item["Type"]!.GetValue<string>().Should().Be("System");
        item["Title"]!.GetValue<string>().Should().StartWith("Notificação seed");
        item["Body"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
        item["Read"]!.GetValue<bool>().Should().BeFalse();
        item["ReadAt"].Should().BeNull();
        item["CreatedAt"].Should().NotBeNull();
    }

    [Fact]
    public async Task Unread_count_and_mark_read_flow()
    {
        var user = await CreateUserAsync();
        var seeded = await OffChainSeed.InsertNotificationsAsync(Factory, user.UserId, count: 2);
        var client = AuthedClient(user);

        var countBefore = await client.GetFromJsonAsync<int>("/api/notifications/unread-count");
        countBefore.Should().Be(2);

        // Marca a primeira como read → 204 sem body.
        var mark = await client.PostAsync($"/api/notifications/{seeded[0].Id}/read", content: null);
        mark.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var countAfter = await client.GetFromJsonAsync<int>("/api/notifications/unread-count");
        countAfter.Should().Be(1);

        // Filtro unreadOnly=true devolve só a pendente.
        var unread = await client.GetAsync("/api/notifications?unreadOnly=true");
        unread.StatusCode.Should().Be(HttpStatusCode.OK);
        var unreadArr = await unread.Content.ReadFromJsonAsync<JsonArray>();
        unreadArr!.Select(n => n!["Id"]!.GetValue<Guid>())
            .Should().ContainSingle().Which.Should().Be(seeded[1].Id);

        // Idempotência de leitura: marcar de novo → 400 de domínio.
        var remark = await client.PostAsync($"/api/notifications/{seeded[0].Id}/read", content: null);
        remark.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var remarkJson = await remark.Content.ReadFromJsonAsync<JsonNode>();
        remarkJson!["Error"]!.GetValue<string>().Should().Contain("read");
    }

    [Fact]
    public async Task Mark_read_rejects_foreign_and_unknown_notification()
    {
        var owner = await CreateUserAsync();
        var seeded = await OffChainSeed.InsertNotificationsAsync(Factory, owner.UserId, count: 1);

        // Outro usuário NÃO marca a notificação alheia — mensagem neutra (anti-enumeração).
        var intruder = await CreateUserAsync();
        var foreign = await AuthedClient(intruder).PostAsync(
            $"/api/notifications/{seeded[0].Id}/read", content: null);
        foreign.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var foreignJson = await foreign.Content.ReadFromJsonAsync<JsonNode>();
        foreignJson!["Error"]!.GetValue<string>().Should().Contain("encontrada");

        var unknown = await AuthedClient(owner).PostAsync(
            $"/api/notifications/{Guid.NewGuid()}/read", content: null);
        unknown.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var unknownJson = await unknown.Content.ReadFromJsonAsync<JsonNode>();
        unknownJson!["Error"]!.GetValue<string>().Should().Contain("encontrada");
    }

    [Fact]
    public async Task Push_subscribe_and_unsubscribe_roundtrip()
    {
        var user = await CreateUserAsync();
        var client = AuthedClient(user);
        var endpoint = $"https://push.example/e2e/{Guid.NewGuid():N}";

        var subscribe = await client.PostAsync("/api/notifications/push/subscribe", JsonBody(new
        {
            Endpoint = endpoint,
            P256dh = "chave-publica-e2e",
            Auth = "segredo-e2e",
        }));
        subscribe.StatusCode.Should().Be(HttpStatusCode.OK);
        var subscriptionId = await ReadIdAsync(subscribe);
        subscriptionId.Should().NotBeEmpty();

        // Remove via query string (logout do dispositivo) → 204.
        var unsubscribe = await client.DeleteAsync($"/api/notifications/push/subscribe?endpoint={Uri.EscapeDataString(endpoint)}");
        unsubscribe.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Sem endpoint → 400 de validação do controller.
        var noEndpoint = await client.DeleteAsync("/api/notifications/push/subscribe");
        noEndpoint.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await noEndpoint.Content.ReadFromJsonAsync<JsonNode>();
        json!["Error"]!.GetValue<string>().Should().Contain("Endpoint");
    }

    [Fact]
    public async Task Push_subscribe_rejects_invalid_endpoint_with_field_errors()
    {
        var user = await CreateUserAsync();

        var resp = await AuthedClient(user).PostAsync("/api/notifications/push/subscribe", JsonBody(new
        {
            Endpoint = "ftp://nao-e-http",
            P256dh = "chave",
            Auth = "segredo",
        }));

        // Envelope de validação do pipeline: Error + Errors[{Field, Message}].
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        json!["Error"]!.GetValue<string>().Should().Be("Dados inválidos.");
        var errors = json["Errors"]!.AsArray();
        errors.Should().Contain(e => e!["Field"]!.GetValue<string>() == "Endpoint");
    }
}
