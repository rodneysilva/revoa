using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Revoa.Application;
using Revoa.Infrastructure.Persistence;
using Revoa.Demurrage.Application.Commands;
using Revoa.Demurrage.Application.Options;
using Revoa.Demurrage.Application.Services;
using Revoa.Demurrage.Domain.Repositories;
using Revoa.Demurrage.Infrastructure.Persistence;

namespace Revoa.Demurrage.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDemurrageInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var conn = configuration["Mongo:ConnectionString"] ?? "mongodb://localhost:27017";
        var dbName = configuration["Mongo:Database"] ?? "revoa";

        // Mongo client/database compartilhados (TryAdd: não duplica se outro módulo já registrou).
        services.TryAddSingleton<IMongoClient>(_ => new MongoClient(conn));
        services.TryAddScoped<IMongoDatabase>(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(dbName));

        // Repositório (coleção própria: DemurrageRuns).
        services.AddScoped<IDemurrageRunRepository, DemurrageRunsRepository>();

        // Parâmetros admin-configuráveis (taxa mensal, piso de isenção, liga/desliga).
        services.Configure<DemurrageOptions>(configuration.GetSection(DemurrageOptions.SectionName));

        // CQRS — MediatR (assembly da Application). IRvmService e IWalletAddressReader são
        // resolvidos dos módulos Token/Account (portas); não há registro duplicado aqui.
        // Índices criados no startup via IMongoIndexEnsurer (loop no Program.cs).
        services.AddScoped<IMongoIndexEnsurer>(sp => (IMongoIndexEnsurer)sp.GetRequiredService<IDemurrageRunRepository>());

        services.AddRevoaCQRS(typeof(RunDemurrageCommandHandler).Assembly);

        // IPCA (BCB série 433) via typed client + scheduler mensal: run automático no dia 1º
        // 03:00 UTC; nos trimestres aplica antes o reajuste IPCA na taxa runtime. Sem Quartz.
        services.AddHttpClient<IIpcaReader, BcbIpcaReader>();
        services.AddHostedService<DemurrageSchedulerService>();

        return services;
    }
}
