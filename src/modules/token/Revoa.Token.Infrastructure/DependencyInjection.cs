using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Revoa.Application;
using Revoa.Token.Application.EventHandlers;
using Revoa.Token.Application.Services;
using Revoa.Token.Infrastructure.Services;

namespace Revoa.Token.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTokenInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ChainOptions>(configuration.GetSection(ChainOptions.SectionName));

        // Scoped: cada request tem sua Web3/account (isola nonce entre requests concorrentes).
        services.AddScoped<IRvmService, NethereumRvmService>();

        // CQRS — registra o handler de WalletCreatedEvent no MediatR (compartilha o barramento).
        services.AddRevoaCQRS(typeof(WalletCreatedEventHandler).Assembly);

        return services;
    }
}
