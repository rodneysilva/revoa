using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Revoa.Demurrage.Application.Commands;
using Revoa.Demurrage.Application.Options;
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
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RunDemurrageCommandHandler).Assembly));

        return services;
    }
}
