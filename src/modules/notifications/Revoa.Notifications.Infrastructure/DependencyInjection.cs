using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Revoa.IntegrationContracts.Notifications;
using Revoa.Notifications.Application.Commands;
using Revoa.Notifications.Application.Services;
using Revoa.Notifications.Application.Validators;
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
        services.AddScoped<INotifier, Notifier>();
        services.AddScoped<IWebPushSender, WebPushService>();

        // CQRS — MediatR (assembly da Application, onde vivem command/query handlers e event handlers)
        // + pipeline de validação. Isso registra DonationCompletedEventHandler (INotificationHandler<>).
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(SubscribePushCommandHandler).Assembly);
            cfg.AddOpenBehavior(typeof(Application.Behaviors.ValidationBehavior<,>));
        });

        // Validators (FluentValidation).
        services.AddValidatorsFromAssembly(typeof(SubscribePushCommandValidator).Assembly);

        return services;
    }
}
