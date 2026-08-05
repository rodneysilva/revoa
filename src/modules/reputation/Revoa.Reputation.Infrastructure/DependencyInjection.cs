using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Revoa.Reputation.Application.EventHandlers;
using Revoa.Reputation.Application.Options;
using Revoa.Reputation.Domain.Repositories;
using Revoa.Reputation.Infrastructure.Persistence;

namespace Revoa.Reputation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddReputationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var conn = configuration["Mongo:ConnectionString"] ?? "mongodb://localhost:27017";
        var dbName = configuration["Mongo:Database"] ?? "revoa";

        // Mongo client/database compartilhados (TryAdd: não duplica se outro módulo já registrou).
        services.TryAddSingleton<IMongoClient>(_ => new MongoClient(conn));
        services.TryAddScoped<IMongoDatabase>(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(dbName));

        // Repositório (coleção própria: Reputations).
        services.AddScoped<IReputationRepository, ReputationsRepository>();

        // Parâmetros admin-configuráveis da recompensa de doação.
        services.Configure<DonationRewardOptions>(configuration.GetSection(DonationRewardOptions.SectionName));

        // CQRS — MediatR (assembly da Application, onde vive DonationCompletedEventHandler).
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DonationCompletedEventHandler).Assembly));

        return services;
    }
}
