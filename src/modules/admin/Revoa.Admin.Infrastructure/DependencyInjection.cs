using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Revoa.Admin.Application.Queries;
using Revoa.Admin.Domain.Repositories;
using Revoa.Admin.Infrastructure.Persistence;
using Revoa.Application;
using Revoa.IntegrationContracts.Admin;

namespace Revoa.Admin.Infrastructure;

public static class DependencyInjection
{
    // Registra o módulo Admin (UF-30): Mongo compartilhado, repositório SystemParameters, a porta
    // IParameterStore (consumida pelos módulos Reputation/Demurrage/Pricing) e o MediatR da
    // Application. Deve ser registrado antes do app.Build(); a ordem relativa aos demais módulos
    // não importa (a resolução da porta é por-request/Scoped).
    public static IServiceCollection AddAdminInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var conn = configuration["Mongo:ConnectionString"] ?? "mongodb://localhost:27017";
        var dbName = configuration["Mongo:Database"] ?? "revoa";

        // Mongo client/database compartilhados (TryAdd: não duplica se outro módulo já registrou).
        services.TryAddSingleton<IMongoClient>(_ => new MongoClient(conn));
        services.TryAddScoped<IMongoDatabase>(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(dbName));

        // Repositório (coleção própria: SystemParameters).
        services.AddScoped<ISystemParameterRepository, SystemParametersRepository>();

        // Porta de parâmetros runtime — consumida pelos demais módulos via IParameterStore.
        services.AddScoped<IParameterStore, ParameterStore>();

        // CQRS — MediatR (assembly da Application).
        services.AddRevoaCQRS(typeof(GetAllParametersQueryHandler).Assembly);

        return services;
    }
}
