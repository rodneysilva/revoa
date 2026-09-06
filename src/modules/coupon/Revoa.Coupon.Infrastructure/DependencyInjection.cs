using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Revoa.Application;
using Revoa.Coupon.Application.Commands;
using Revoa.Coupon.Application.Services;
using Revoa.Coupon.Domain.Repositories;
using Revoa.Coupon.Infrastructure.Persistence;
using Revoa.Coupon.Infrastructure.Services;

namespace Revoa.Coupon.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCouponInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var conn = configuration["Mongo:ConnectionString"] ?? "mongodb://localhost:27017";
        var dbName = configuration["Mongo:Database"] ?? "revoa";

        // Mongo client/database compartilhados (TryAdd: não duplica se outro módulo já registrou).
        services.TryAddSingleton<IMongoClient>(_ => new MongoClient(conn));
        services.TryAddScoped<IMongoDatabase>(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(dbName));

        // Repositório (coleção própria Coupons, isolada dos demais módulos).
        services.AddScoped<ICouponRepository, CouponsRepository>();

        // Chain (CouponRedeemer). Orquestração on-chain de criar/revogar/resgatar cupom.
        services.Configure<CouponChainOptions>(configuration.GetSection(CouponChainOptions.SectionName));
        services.AddScoped<ICouponChainService, NethereumCouponChainService>();

        // CQRS — MediatR (assembly da Application, onde vivem os handlers de command/query).
        services.AddRevoaCQRS(typeof(CreateCouponCommandHandler).Assembly);

        return services;
    }
}
