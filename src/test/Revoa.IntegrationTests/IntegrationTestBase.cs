using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests;

// Base dos testes de integração: acessa a ApiFactory compartilhada, expõe um HttpClient, faz o
// reset do Mongo por teste (IAsyncLifetime) e concentra helpers de auth (register/dev-verify/login).
//
// JSON: a API serializa em PascalCase (PropertyNamingPolicy=null em Program.cs). Os requests são
// montados em PascalCase; as leituras usam JsonNode (preserva o casing original do payload).
[Collection("Integration")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    // PascalCase (compatível com o serializer do app). Case-insensitive para tolerância na leitura.
    private static readonly JsonSerializerOptions WriteJson = new()
    {
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = true,
    };

    protected ApiFactory Factory { get; }
    protected HttpClient Http { get; }
    protected IConfiguration Config => Factory.Configuration;

    protected IntegrationTestBase(ApiFactory factory)
    {
        Factory = factory;
        Http = factory.CreateClient();
    }

    public virtual async Task InitializeAsync()
    {
        // Estado pristine antes de cada teste (mantém o seed de categorias).
        await Factory.ResetDatabaseAsync();
    }

    public virtual Task DisposeAsync() => Task.CompletedTask;

    // ---- Helpers de baixo nível ----

    protected static StringContent JsonBody(object data) =>
        new(JsonSerializer.Serialize(data, WriteJson), Encoding.UTF8, "application/json");

    protected async Task<JsonNode> ReadJsonAsync(HttpResponseMessage resp)
    {
        resp.IsSuccessStatusCode.Should().BeTrue(
            $"esperado sucesso, mas veio {(int)resp.StatusCode}: {await resp.Content.ReadAsStringAsync()}");
        var node = await resp.Content.ReadFromJsonAsync<JsonNode>();
        node.Should().NotBeNull("o corpo da resposta não deveria ser vazio");
        return node!;
    }

    // Id do recurso criado: todo sucesso de criação/ação devolve o envelope { "Id": "<guid>" }
    // (ResourceId, ADR-0017) — nunca um guid cru em text/plain. Leitura via JsonNode: o record
    // tem 2 ctors e o System.Text.Json não desserializa ResourceId direto (sem [JsonConstructor]).
    protected static async Task<Guid> ReadIdAsync(HttpResponseMessage resp)
    {
        var node = await resp.Content.ReadFromJsonAsync<JsonNode>();
        node.Should().NotBeNull(
            $"esperado envelope {{\"Id\":...}}: {await resp.Content.ReadAsStringAsync()}");
        return node!["Id"]!.GetValue<Guid>();
    }

    // ---- Auth ----

    // POST /api/auth/register (anônimo). Retorna o UserId do usuário recém-criado.
    protected async Task<Guid> RegisterAsync(string nome, string email, string? telefone = null)
    {
        telefone ??= $"+5511{Random.Shared.Next(100_000_000, 999_999_999)}";
        var resp = await Http.PostAsync("/api/auth/register", JsonBody(new
        {
            Name = nome,
            Email = email,
            Phone = telefone,
            BirthDate = "1990-01-01",
            CouponCode = (string?)null,
        }));

        var json = await ReadJsonAsync(resp);
        return json["UserId"]!.GetValue<Guid>();
    }

    // POST /api/auth/dev-verify (DEV-ONLY): ativa e-mail+telefone e devolve o JWT. Atalho de teste.
    protected async Task<TestUser> DevVerifyAsync(string email, string nome)
    {
        var resp = await Http.PostAsync("/api/auth/dev-verify", JsonBody(new { Email = email }));
        var json = await ReadJsonAsync(resp);
        return new TestUser(
            email,
            json["Token"]!.GetValue<string>(),
            json["UserId"]!.GetValue<Guid>(),
            nome);
    }

    // POST /api/auth/login (DEV-ONLY): emite JWT para usuário já verificado.
    protected async Task<string> LoginAsync(string email)
    {
        var resp = await Http.PostAsync("/api/auth/login", JsonBody(new { Email = email }));
        var json = await ReadJsonAsync(resp);
        return json["Token"]!.GetValue<string>();
    }

    // Fluxo completo de teste: registra um usuário comum verificado (Bearer pronto).
    protected async Task<TestUser> CreateUserAsync(string? email = null, string? nome = null)
    {
        email ??= $"e2e-{Guid.NewGuid():N}@revoa.test";
        nome ??= "Usuario E2E";
        await RegisterAsync(nome, email);
        return await DevVerifyAsync(email, nome);
    }

    // Admin: e-mail já configurado em Admin:Emails do appsettings.Development.json. Registra,
    // dev-verifica e devolve — o JWT sai com a claim de e-mail que satisfaz a policy "Admin".
    protected async Task<TestUser> CreateAdminAsync() =>
        await CreateUserAsync(email: "rodneydocarmo@gmail.com", nome: "Admin E2E");

    // HttpClient autenticado (Bearer) para um usuário.
    protected HttpClient AuthedClient(TestUser user)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        return client;
    }

    // ---- Chain ----

    // Pula o teste graceful se a chain de dev (anvil + contratos) não estiver acessível.
    // Usado apenas por testes on-chain (CouponsController/Troca/Doação). Os off-chain não chamam.
    protected void SkipIfChainUnavailable()
    {
        Skip.IfNot(ChainProbe.IsAvailable(Config), "anvil indisponível (chain de dev não acessível)");
    }

    // ---- Helpers de domínio ----

    // Primeiro id de categoria do seed (para criar anúncios). O seed garante 9 categorias ativas.
    protected async Task<Guid> GetFirstCategoryIdAsync()
    {
        var resp = await Http.GetAsync("/api/categories");
        var arr = await resp.Content.ReadFromJsonAsync<JsonArray>();
        arr.Should().NotBeNullOrEmpty("deveria haver categorias via seed");
        return arr![0]!["Id"]!.GetValue<Guid>();
    }

    // Cria comunidade User/Open (para testes de feed/moderação admin) e devolve o Id.
    protected async Task<Guid> CreateCommunityAsync(TestUser creator)
    {
        var resp = await AuthedClient(creator).PostAsync("/api/communities", JsonBody(new
        {
            Name = $"Comunidade E2E {Guid.NewGuid():N}",
            Description = "Descrição E2E",
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
        resp.IsSuccessStatusCode.Should().BeTrue(
            $"esperado 2xx ao criar comunidade: {await resp.Content.ReadAsStringAsync()}");
        return await ReadIdAsync(resp);
    }

    // Cria post raiz em uma comunidade (autor precisa de vínculo Active) e devolve o PostId.
    protected async Task<Guid> CreateCommunityPostAsync(TestUser autor, Guid communityId, string conteudo)
    {
        var resp = await AuthedClient(autor).PostAsync(
            $"/api/communities/{communityId}/posts", JsonBody(new
            {
                ParentId = (Guid?)null,
                Content = conteudo,
            }));
        resp.IsSuccessStatusCode.Should().BeTrue(
            $"esperado 2xx ao criar post: {await resp.Content.ReadAsStringAsync()}");
        return await ReadIdAsync(resp);
    }
}
