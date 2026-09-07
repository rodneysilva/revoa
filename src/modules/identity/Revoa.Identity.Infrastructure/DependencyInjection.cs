using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Revoa.Application;
using Revoa.Identity.Application.Commands;
using Revoa.Identity.Application.Services;
using Revoa.Identity.Domain.Repositories;
using Revoa.Identity.Infrastructure.Persistence;
using Revoa.Identity.Infrastructure.Services;
using Revoa.Infrastructure.Persistence;

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

        // IMongoClient instrumentado (tracing de comandos) é registrado em Program.cs ANTES dos
        // módulos. TryAdd evita sobrescrever o cliente compartilhado já registrado; funciona
        // também quando o módulo é usado isoladamente (ex.: host de testes).
        services.TryAddSingleton<IMongoClient>(_ => new MongoClient(conn));
        services.TryAddScoped<IMongoDatabase>(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(dbName));

        services.AddScoped<IUserRepository, UsersRepository>();
        services.AddScoped<IUnitOfWork, MongoDbUnitOfWork>();

        // E-mail — ADR-0014. API do Brevo quando a chave v3 está configurada;
        // senão MailKit -> SMTP (Postfix interno ou relay autenticado).
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        if (!string.IsNullOrWhiteSpace(configuration["Email:ApiKey"]))
        {
            services.AddHttpClient<BrevoApiEmailSender>();
            services.AddScoped<IEmailSender>(sp => sp.GetRequiredService<BrevoApiEmailSender>());
        }
        else
        {
            services.AddScoped<IEmailSender, MailKitEmailSender>();
        }

        // Hash de OTP (login/telefone): HMAC-SHA256 com chave do servidor — SHA256 puro é
        // quebrável offline (900k combinações) com só leitura do banco. Fail-fast sem chave.
        var otpSecret = configuration["Email:OtpHashKey"];
        if (string.IsNullOrWhiteSpace(otpSecret))
        {
            otpSecret = configuration["Jwt:Key"];
        }
        if (string.IsNullOrWhiteSpace(otpSecret))
        {
            throw new InvalidOperationException(
                "Hash de OTP exige Email:OtpHashKey ou Jwt:Key configurado (código de 6 dígitos nunca vai sem chave).");
        }
        services.AddSingleton<IOtpHasher>(new HmacOtpHasher(otpSecret));

        // WhatsApp/SMS (Zenvia) — ADR-0013
        services.Configure<ZenviaOptions>(configuration.GetSection(ZenviaOptions.SectionName));
        services.AddHttpClient<ZenviaSmsSender>();
        services.AddScoped<ISmsSender>(sp => sp.GetRequiredService<ZenviaSmsSender>());

        // Índices criados no startup via IMongoIndexEnsurer (loop no Program.cs).
        services.AddScoped<IMongoIndexEnsurer>(sp => (IMongoIndexEnsurer)sp.GetRequiredService<IUserRepository>());

        // CQRS — MediatR (assembly da Application) + pipeline de validação (roda os validators)
        services.AddRevoaCQRS(typeof(RegisterUserCommandHandler).Assembly);

        return services;
    }
}
