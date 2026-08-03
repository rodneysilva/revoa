using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Revoa.Identity.Application.Commands;
using Revoa.Identity.Application.Services;
using Revoa.Identity.Application.Validators;
using Revoa.Identity.Domain.Repositories;
using Revoa.Identity.Infrastructure.Persistence;
using Revoa.Identity.Infrastructure.Services;

namespace Revoa.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
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

        services.AddSingleton<IMongoClient>(_ => new MongoClient(conn));
        services.AddScoped<IMongoDatabase>(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(dbName));

        services.AddScoped<IUserRepository, UsersRepository>();
        services.AddScoped<IUnitOfWork, MongoDbUnitOfWork>();

        // E-mail (MailKit -> Postfix) — ADR-0014
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddScoped<IEmailSender, MailKitEmailSender>();

        // WhatsApp/SMS (Zenvia) — ADR-0013
        services.Configure<ZenviaOptions>(configuration.GetSection(ZenviaOptions.SectionName));
        services.AddHttpClient<ZenviaSmsSender>();
        services.AddScoped<ISmsSender>(sp => sp.GetRequiredService<ZenviaSmsSender>());

        // CQRS — MediatR (assembly da Application) + pipeline de validação (roda os validators)
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(RegisterUserCommandHandler).Assembly);
            cfg.AddOpenBehavior(typeof(Revoa.Identity.Application.Behaviors.ValidationBehavior<,>));
        });

        // Validators (FluentValidation)
        services.AddValidatorsFromAssembly(typeof(RegisterUserCommandValidator).Assembly);

        return services;
    }
}
