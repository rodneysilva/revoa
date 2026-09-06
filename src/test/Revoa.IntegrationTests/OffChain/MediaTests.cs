using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Upload/serve de mídia (MinIO via Testcontainers). O upload é o que o picker do
// CreateListingPage chama; o GET é o que a UI usa como src das imagens do anúncio.
// Cobertura: auth obrigatória, round-trip PNG ({Url} → image/png), rejeição de
// conteúdo não-imagem e 404 limpo para chave inexistente.
public class MediaTests : IntegrationTestBase
{
    public MediaTests(ApiFactory factory) : base(factory) { }

    // PNG válido mínimo: assinatura + início do chunk IHDR (o MinIO não valida o
    // conteúdo — só transporta; o que importa é o content type do round-trip).
    private static readonly byte[] PngBytes =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
    ];

    private static MultipartFormDataContent PngForm()
    {
        var file = new ByteArrayContent(PngBytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var form = new MultipartFormDataContent();
        form.Add(file, "file", "foto.png");
        return form;
    }

    [Fact]
    public async Task Upload_requires_auth()
    {
        var resp = await Http.PostAsync("/api/media", PngForm());
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Upload_returns_url_and_get_serves_the_bytes()
    {
        var user = await CreateUserAsync();
        var client = AuthedClient(user);

        var post = await client.PostAsync("/api/media", PngForm());
        post.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await post.Content.ReadFromJsonAsync<JsonNode>();
        var url = json!["Url"]!.GetValue<string>();
        url.Should().StartWith($"/api/media/listings/{user.UserId}", "a chave é escopada por usuário");
        url.Should().EndWith(".png");

        // GET anônimo (a imagem embute em <img>, sem Bearer) serve o objeto intacto.
        var get = await Http.GetAsync(url);
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        get.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        (await get.Content.ReadAsByteArrayAsync()).Should().Equal(PngBytes);
    }

    [Fact]
    public async Task Upload_rejects_non_image_content_type()
    {
        var user = await CreateUserAsync();
        var client = AuthedClient(user);

        var file = new ByteArrayContent([0x74, 0x65, 0x78, 0x74, 0x6F]); // "texto"
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        var form = new MultipartFormDataContent();
        form.Add(file, "file", "notas.txt");

        var resp = await client.PostAsync("/api/media", form);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_unknown_key_returns_404()
    {
        var resp = await Http.GetAsync("/api/media/listings/nada-aqui.png");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
