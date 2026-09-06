using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Revoa.Application;
using Revoa.Infrastructure.Persistence;
using Revoa.Moderation.Application.Commands;
using Revoa.Moderation.Domain.Repositories;
using Revoa.Moderation.Infrastructure.Persistence;

namespace Revoa.Moderation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddModerationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var conn = configuration["Mongo:ConnectionString"] ?? "mongodb://localhost:27017";
        var dbName = configuration["Mongo:Database"] ?? "revoa";

        // Mongo client/database compartilhados (TryAdd: não duplica se outro módulo já registrou).
        services.TryAddSingleton<IMongoClient>(_ => new MongoClient(conn));
        services.TryAddScoped<IMongoDatabase>(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(dbName));

        // Repositório (coleção própria Reports, isolada dos demais módulos).
        services.AddScoped<IReportRepository, ReportsRepository>();

        // Índices criados no startup via IMongoIndexEnsurer (loop no Program.cs).
        services.AddScoped<IMongoIndexEnsurer>(sp => (IMongoIndexEnsurer)sp.GetRequiredService<IReportRepository>());

        // CQRS — MediatR (assembly da Application, onde vivem os handlers de command/query).
        services.AddRevoaCQRS(typeof(CreateReportCommandHandler).Assembly);

        return services;
    }
}
