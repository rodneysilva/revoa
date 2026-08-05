using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Revoa.Account.Application.EventHandlers;
using Revoa.Account.Domain.Repositories;
using Revoa.Account.Infrastructure.Persistence;
using Revoa.Account.Infrastructure.Services;
using Revoa.IntegrationContracts.Accounts;
using Revoa.IntegrationContracts.UserWallets;

namespace Revoa.Account.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAccountInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string? mongoConnectionString = null,
        string? mongoDatabase = null)
    {
        var conn = mongoConnectionString
                   ?? configuration["Mongo:ConnectionString"]
                   ?? "mongodb://localhost:27017";

        var dbName = mongoDatabase
                     ?? configuration["Mongo:Database"]
                     ?? "revoa";

        // Mongo client/database são compartilhados (TryAdd: não duplica se Identity já registrou).
        services.TryAddSingleton<IMongoClient>(_ => new MongoClient(conn));
        services.TryAddScoped<IMongoDatabase>(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(dbName));

        services.AddScoped<IAccountRepository, AccountsRepository>();

        // Porta IUserWalletProvider: expõe as credenciais da carteira a outros módulos (Exchange)
        // sem quebrar o isolamento de coleções. Adapter lê o aggregate UserAccount.
        services.AddScoped<IUserWalletProvider, AccountWalletProvider>();

        // Porta IWalletAddressReader: lista endereços (Demurrage) sem acessar a coleção Accounts.
        services.AddScoped<IWalletAddressReader, WalletAddressReader>();

        // CQRS — registra o handler de UserRegisteredEvent no MediatR (compartilha o barramento).
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(UserRegisteredEventHandler).Assembly));

        return services;
    }
}

