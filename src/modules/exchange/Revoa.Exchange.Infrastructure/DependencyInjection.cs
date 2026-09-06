using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Revoa.Application;
using Revoa.Exchange.Application.Commands;
using Revoa.Exchange.Application.Services;
using Revoa.Exchange.Domain.Repositories;
using Revoa.Exchange.Infrastructure.Persistence;
using Revoa.Exchange.Infrastructure.Services;

namespace Revoa.Exchange.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddExchangeInfrastructure(
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

        // Mongo client/database compartilhados (TryAdd: não duplica se outro módulo já registrou).
        services.TryAddSingleton<IMongoClient>(_ => new MongoClient(conn));
        services.TryAddScoped<IMongoDatabase>(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(dbName));

        // Repositórios (coleções próprias: Trades, HelpRequests).
        services.AddScoped<ITradeRepository, TradesRepository>();
        services.AddScoped<IHelpRequestRepository, HelpRequestsRepository>();

        // Porta ITradeInfoProvider: expõe contexto da troca (estado + partes) p/ o módulo
        // Reputation avaliar pós-troca (UF-23) sem acessar a coleção Trades (isolamento).
        services.AddScoped<Revoa.IntegrationContracts.Trades.ITradeInfoProvider, TradeInfoProvider>();

        // Chain (EscrowVault + ServiceVoucher + RVM approve). Orquestração on-chain do escrow.
        services.Configure<ExchangeChainOptions>(configuration.GetSection(ExchangeChainOptions.SectionName));
        services.AddScoped<IExchangeEscrowService, NethereumExchangeEscrowService>();

        // CQRS — MediatR (assembly da Application) + pipeline de validação + validators.
        services.AddRevoaCQRS(typeof(PurchaseCommandHandler).Assembly);

        return services;
    }
}
