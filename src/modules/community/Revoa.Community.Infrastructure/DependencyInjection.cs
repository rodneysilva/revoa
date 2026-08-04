using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Revoa.Community.Application.Commands;
using Revoa.Community.Application.Validators;
using Revoa.Community.Domain.Repositories;
using Revoa.Community.Infrastructure.Persistence;

namespace Revoa.Community.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCommunityInfrastructure(
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

        // Repositórios (coleções próprias: Communities, Memberships, Posts, ChatMessages).
        services.AddScoped<ICommunityRepository, CommunitiesRepository>();
        services.AddScoped<IMembershipRepository, MembershipsRepository>();
        services.AddScoped<IPostRepository, PostsRepository>();
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();

        // CQRS — MediatR (assembly da Application) + pipeline de validação.
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateCommunityCommandHandler).Assembly);
            cfg.AddOpenBehavior(typeof(Application.Behaviors.ValidationBehavior<,>));
        });

        // Validators (FluentValidation).
        services.AddValidatorsFromAssembly(typeof(CreateCommunityCommandValidator).Assembly);

        return services;
    }
}
