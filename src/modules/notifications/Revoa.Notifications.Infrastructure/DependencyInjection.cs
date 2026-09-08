using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Revoa.Application;
using Revoa.Infrastructure.Persistence;
using Revoa.IntegrationContracts.Notifications;
using Revoa.Notifications.Application.Commands;
using Revoa.Notifications.Application.Services;
using Revoa.Notifications.Domain.Repositories;
using Revoa.Notifications.Infrastructure.Persistence;

namespace Revoa.Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsInfrastructure(
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

        // Repositórios (coleções próprias: Notifications, PushSubscriptions).
        services.AddScoped<INotificationRepository, NotificationsRepository>();
        services.AddScoped<IPushSubscriptionRepository, PushSubscriptionsRepository>();

        // VAPID (Web Push). Vazio em dev → serviço resiliencia (só in-app SignalR).
        services.Configure<VapidOptions>(configuration.GetSection(VapidOptions.SectionName));

        // Portas: INotifier (p/ outros módulos, anti-corruption) + IWebPushSender (p/ Notifier).
        // WebPush usa typed client (IHttpClientFactory) — POSTs ao push service do navegador.
        services.AddScoped<INotifier, Notifier>();
        services.AddHttpClient<IWebPushSender, WebPushService>();

        // Índices criados no startup via IMongoIndexEnsurer (loop no Program.cs).
        services.AddScoped<IMongoIndexEnsurer>(sp => (IMongoIndexEnsurer)sp.GetRequiredService<INotificationRepository>());
        services.AddScoped<IMongoIndexEnsurer>(sp => (IMongoIndexEnsurer)sp.GetRequiredService<IPushSubscriptionRepository>());

        // CQRS — MediatR (assembly da Application, onde vivem command/query handlers e event handlers)
        // + pipeline de validação. Isso registra DonationCompletedEventHandler (INotificationHandler<>).
        services.AddRevoaCQRS(typeof(SubscribePushCommandHandler).Assembly);

        return services;
    }
}
