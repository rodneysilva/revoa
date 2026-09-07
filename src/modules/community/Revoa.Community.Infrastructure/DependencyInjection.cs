using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Revoa.Application;
using Revoa.Community.Application.Commands;
using Revoa.Community.Domain.Repositories;
using Revoa.Community.Infrastructure.Persistence;
using Revoa.Community.Infrastructure.Services;
using Revoa.IntegrationContracts.Communities;
using Revoa.Infrastructure.Persistence;

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

        // Repositórios (coleções próprias: Communities, Memberships, Posts, ChatMessages,
        // PostLikes, SavedPosts).
        services.AddScoped<ICommunityRepository, CommunitiesRepository>();
        services.AddScoped<IMembershipRepository, MembershipsRepository>();
        services.AddScoped<IPostRepository, PostsRepository>();
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
        services.AddScoped<IPostLikeRepository, PostLikesRepository>();
        services.AddScoped<ISavedPostRepository, SavedPostsRepository>();

        // Índices criados no startup via IMongoIndexEnsurer (loop no Program.cs).
        services.AddScoped<IMongoIndexEnsurer>(sp => (IMongoIndexEnsurer)sp.GetRequiredService<ICommunityRepository>());
        services.AddScoped<IMongoIndexEnsurer>(sp => (IMongoIndexEnsurer)sp.GetRequiredService<IMembershipRepository>());
        services.AddScoped<IMongoIndexEnsurer>(sp => (IMongoIndexEnsurer)sp.GetRequiredService<IPostRepository>());
        services.AddScoped<IMongoIndexEnsurer>(sp => (IMongoIndexEnsurer)sp.GetRequiredService<IChatMessageRepository>());
        services.AddScoped<IMongoIndexEnsurer>(sp => (IMongoIndexEnsurer)sp.GetRequiredService<IPostLikeRepository>());
        services.AddScoped<IMongoIndexEnsurer>(sp => (IMongoIndexEnsurer)sp.GetRequiredService<ISavedPostRepository>());

        // CQRS — MediatR (assembly da Application) + pipeline de validação + validators.
        services.AddRevoaCQRS(typeof(CreateCommunityCommandHandler).Assembly);

        // Porta cross-módulo: Catalog valida vínculo Active antes de escopar anúncio.
        services.AddScoped<IMembershipStatusChecker, MembershipStatusChecker>();

        return services;
    }
}
