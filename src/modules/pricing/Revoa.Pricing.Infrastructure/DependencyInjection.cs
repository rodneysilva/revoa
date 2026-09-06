using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Revoa.Application;
using Revoa.Pricing.Application.Commands;
using Revoa.Pricing.Application.Options;
using Revoa.Pricing.Domain.Repositories;
using Revoa.Pricing.Infrastructure.Persistence;

namespace Revoa.Pricing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPricingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var conn = configuration["Mongo:ConnectionString"] ?? "mongodb://localhost:27017";
        var dbName = configuration["Mongo:Database"] ?? "revoa";

        // Mongo client/database compartilhados (TryAdd: não duplica se outro módulo já registrou).
        services.TryAddSingleton<IMongoClient>(_ => new MongoClient(conn));
        services.TryAddScoped<IMongoDatabase>(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(dbName));

        // Repositório (coleção própria: PriceReferences).
        services.AddScoped<IPriceReferenceRepository, PriceReferencesRepository>();

        // Parâmetros admin-configuráveis (BrlRate, BrlReferences, Ollama, IBGE).
        services.Configure<PricingOptions>(configuration.GetSection(PricingOptions.SectionName));

        // CQRS — MediatR (assembly da Application).
        services.AddRevoaCQRS(typeof(RefreshPricingCommandHandler).Assembly);

        // HttpClient tipado p/ o handler (IBGE + Ollama). Timeout 35s (Ollama qwen2.5:7b ~30s).
        // Registrado APÓS o MediatR para que a ativação via HttpClientFactory prevaleça na resolução
        // do handler concreto (injeção do HttpClient).
        services.AddHttpClient<RefreshPricingCommandHandler>(c => c.Timeout = TimeSpan.FromSeconds(35));

        return services;
    }
}
