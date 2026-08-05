using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Revoa.Catalog.Application.Commands;
using Revoa.Catalog.Application.Services;
using Revoa.Catalog.Application.Validators;
using Revoa.Catalog.Domain.Repositories;
using Revoa.Catalog.Infrastructure.Persistence;
using Revoa.Catalog.Infrastructure.Services;

namespace Revoa.Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogInfrastructure(
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

        // Repositórios (coleções próprias: Listings, Categories, Comments).
        services.AddScoped<IListingRepository, ListingsRepository>();
        services.AddScoped<ICategoryRepository, CategoriesRepository>();
        services.AddScoped<ICommentRepository, CommentsRepository>();

        // ViaCEP (geolocalização por CEP).
        services.AddHttpClient<ViaCepService>();
        services.AddScoped<IViaCepService>(sp => sp.GetRequiredService<ViaCepService>());

        // Porta IListingSummaryProvider: expõe resumo do anúncio ao módulo Exchange (isolamento).
        services.AddScoped<Revoa.IntegrationContracts.Listings.IListingSummaryProvider, ListingSummaryProvider>();

        // Porta IListingPriceReader: expõe amostras de preço (ativos + slug) ao módulo Pricing (isolamento).
        services.AddScoped<Revoa.IntegrationContracts.Pricing.IListingPriceReader, ListingPriceReader>();

        // Chain (ProductNFT mint-to-escrow). Scoped: isola nonce por request.
        services.Configure<CatalogChainOptions>(configuration.GetSection(CatalogChainOptions.SectionName));
        services.AddScoped<IProductNftService, NethereumProductNftService>();

        // CQRS — MediatR (assembly da Application) + pipeline de validação.
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateListingCommandHandler).Assembly);
            cfg.AddOpenBehavior(typeof(Application.Behaviors.ValidationBehavior<,>));
        });

        // Validators (FluentValidation).
        services.AddValidatorsFromAssembly(typeof(CreateListingCommandValidator).Assembly);

        return services;
    }
}
