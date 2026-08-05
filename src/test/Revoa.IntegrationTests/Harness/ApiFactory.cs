using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Revoa.Identity.Application.Services;
using Testcontainers.MongoDb;
using Xunit;

namespace Revoa.IntegrationTests.Harness;

// Sobe a API in-process (WebApplicationFactory<Program>) sobre um Mongo Testcontainers isolado,
// sem tocar o Mongo do host. Um container por execução de testes (collection compartilhada); o
// reset entre testes limpa as coleções mantendo o seed de categorias.
//
// Override de Mongo: o Program.cs registra IMongoClient via TryAddSingleton (captura a conn string
// em tempo de build). Aqui registramos novamente IMongoClient (last-wins no DI) apontando ao
// container. O nome do banco ("revoa") é capturado por cada módulo, mas resolve contra este cliente
// → fica isolado dentro do container.
//
// Ambiente = Development: carrega Chain:* (anvil + contratos deployados), Jwt:Key de dev, Admin:Emails
// (admin em revoa), e libera os endpoints DEV-ONLY (/api/auth/login e /api/auth/dev-verify).
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MongoDbContainer _mongo = new MongoDbBuilder()
        .WithImage("mongo:7")
        .Build();

    public string MongoConnectionString => _mongo.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development: appsettings.Development.json (Chain, Jwt:Key, Admin:Emails) + endpoints dev.
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            // Mongo override (last-wins sobre o TryAddSingleton do Program.cs).
            services.AddSingleton<IMongoClient>(_ => new MongoClient(MongoConnectionString));

            // E-mail silenciado: sem Postfix em testes, o MailKit lançaria e quebraria o registro.
            services.AddScoped<IEmailSender, NoopEmailSender>();
        });
    }

    public async Task InitializeAsync()
    {
        // Container Mongo deve estar pronto ANTES do host ser construído (índices/seed no startup).
        await _mongo.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _mongo.DisposeAsync();
        await base.DisposeAsync();
    }

    // Limpa o banco entre testes: remove todos os documentos de cada coleção EXCETO Categories
    // (preserva o seed das 9 categorias e os índices criados no startup). Deixa o estado pristine.
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IMongoClient>();
        var database = client.GetDatabase("revoa");

        var collections = await (await database.ListCollectionNamesAsync()).ToListAsync();
        foreach (var name in collections)
        {
            if (name == "Categories")
            {
                continue;
            }

            await database
                .GetCollection<BsonDocument>(name)
                .DeleteManyAsync(Builders<BsonDocument>.Filter.Empty);
        }
    }

    public IConfiguration Configuration => Services.GetRequiredService<IConfiguration>();
}
