using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Revoa.Application.Behaviors;

namespace Revoa.Application;

public static class RevoaCqrsExtensions
{
    /// <summary>
    /// CQRS padrão do revoa: MediatR (handlers dos assemblies informados) + pipeline de validação
    /// FluentValidation + registro dos validators — substitui o bloco copiado em cada módulo.
    /// Um chamado por módulo: AddRevoaCQRS(typeof(AlgumCommandHandler).Assembly).
    /// </summary>
    public static IServiceCollection AddRevoaCQRS(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddMediatR(cfg =>
        {
            foreach (var assembly in assemblies)
            {
                cfg.RegisterServicesFromAssembly(assembly);
            }

            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssemblies(assemblies);
        return services;
    }
}
