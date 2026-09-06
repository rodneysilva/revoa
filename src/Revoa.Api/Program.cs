using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.IdentityModel.Tokens;
using Revoa.Abstractions;
using Revoa.Account.Infrastructure;
using Revoa.Admin.Infrastructure;
using Revoa.Api.Hubs;
using Revoa.Catalog.Infrastructure;
using Revoa.Catalog.Infrastructure.Persistence;
using Revoa.Community.Infrastructure;
using Revoa.Coupon.Infrastructure;
using Revoa.Demurrage.Infrastructure;
using Revoa.Exchange.Infrastructure;
using Revoa.Identity.Infrastructure;
using Revoa.Infrastructure;
using Revoa.Infrastructure.Persistence;
using Revoa.Moderation.Infrastructure;
using Revoa.Notifications.Infrastructure;
using Revoa.Notifications.Infrastructure.Hubs;
using Revoa.Pricing.Infrastructure;
using Revoa.Reputation.Infrastructure;
using Revoa.Token.Infrastructure;
using System.Text;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Revoa.Api.Observability;

// Logger Serilog bootstrap: ativo ANTES do host existir, captura erros de startup/configuração.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.With<ActivityLogEnricher>()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Iniciando host Revoa.Api...");

    var builder = WebApplication.CreateBuilder(args);

    // Logger Serilog final: lê a seção "Serilog" do appsettings (+ env vars) e substitui o bootstrap.
    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.With<ActivityLogEnricher>());

    // OpenTelemetry tracing (AspNetCore + HttpClient + MongoDB); exporter via config (Otel:Exporter).
    AddObservability(builder);

    // IMongoClient único e instrumentado (tracing de comandos Mongo). Registrado ANTES dos módulos:
    // todos usam TryAddSingleton, logo este prevalece — sem precisar tocar cada bounded context.
    var mongoConn = builder.Configuration["Mongo:ConnectionString"] ?? "mongodb://localhost:27017";
    builder.Services.TryAddSingleton<IMongoClient>(_ => CreateMongoClient(mongoConn));


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // PascalCase (bate com os tipos do FE)
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()); // enums como string legível ("Ativo","Product")
    });
builder.Services.AddSignalR()
    .AddJsonProtocol(options => options.PayloadSerializerOptions.PropertyNamingPolicy = null);
builder.Services.AddOpenApi();

// Event bus de integração in-process (MVP)
builder.Services.AddScoped<IIntegrationEventBus, InProcessIntegrationEventBus>();

// Admin module (UF-30): store de parâmetros runtime (IParameterStore) + painel admin. Registrado
// cedo — a porta IParameterStore é consumida pelos módulos Reputation/Demurrage/Pricing (resolução
// por-request/Scoped; a ordem de registro não afeta a resolução).
builder.Services.AddAdminInfrastructure(builder.Configuration);

// Identity module: Mongo, repo, UoW, e-mail (MailKit), SMS (Zenvia), MediatR+ValidationBehavior, validators
builder.Services.AddIdentityInfrastructure(builder.Configuration);

// Account module: carteira EOA por usuário (MVP dev) + handler de UserRegisteredEvent.
builder.Services.AddAccountInfrastructure(builder.Configuration);

// Token module: faucet RVM (R$20) via Nethereum + handler de WalletCreatedEvent.
builder.Services.AddTokenInfrastructure(builder.Configuration);

// Catalog module: anúncios (Listings/Categories), ViaCEP, ProductNFT mint-to-escrow.
builder.Services.AddCatalogInfrastructure(builder.Configuration);

// Community module: comunidades, memberships, posts recursivos, chat SignalR (100% off-chain).
builder.Services.AddCommunityInfrastructure(builder.Configuration);

// Exchange module: trocas/doação (escrow on-chain atomic swap), vouchers, fila de doação.
builder.Services.AddExchangeInfrastructure(builder.Configuration);

// Notifications module: in-app SignalR + Web Push (escrow/oferta/transfer/post/chat/doação/preço).
builder.Services.AddNotificationsInfrastructure(builder.Configuration);

// Reputation module: score de reputação + recompensa multi-eixo de doação (reputação + pontos de ajuda + bônus RVM).
builder.Services.AddReputationInfrastructure(builder.Configuration);

// Moderation module: denúncias + resolução admin (arquivar/avisar/banir). Ban de usuário via evento
// (UserBanRequestedEvent → Identity), mantendo os módulos isolados.
builder.Services.AddModerationInfrastructure(builder.Configuration);

// Coupon module: cupom/convite on-chain (CouponRedeemer). Admin cria/revoga; usuário resgata (mint RVM).
builder.Services.AddCouponInfrastructure(builder.Configuration);

// Pricing module: referência de preço justo por categoria (mediana comunitária + BRL seed + IPCA IBGE + Ollama).
builder.Services.AddPricingInfrastructure(builder.Configuration);

// Demurrage module (UF-27): queima periódica de uma % do RVM ocioso (piso de isenção + taxa ajustável).
// Consome IRvmService (porta on-chain do Token) e IWalletAddressReader (porta do Account).
builder.Services.AddDemurrageInfrastructure(builder.Configuration);

// Request timeouts p/ endpoints longos (ex.: demurrage preview/run — 1 balanceOf/tx por carteira).
builder.Services.AddRequestTimeouts();

// JWT bearer (esquema; claim sub -> NameIdentifier). Em PRODUÇÃO a chave é obrigatória (fail-fast);
// em Development aceita um default de dev. Nunca versionar a chave de produção.
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "revoa";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "revoa";

if (string.IsNullOrWhiteSpace(jwtKey))
{
    if (builder.Environment.IsProduction())
    {
        throw new InvalidOperationException(
            "Jwt:Key é obrigatório em produção. Configure via secret/env (Jwt__Key) — nunca versionar.");
    }

    jwtKey = "revoa-dev-key-do-not-use-in-prod-min-32-chars!!"; // somente Development
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Não remapear claims JWT (sub→nameidentifier etc.) — os controllers leem FindFirst("sub"/"name"/"avatar").
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = "sub",
            // Claims "role" (emitidas pelo AuthController) alimentam as policies baseadas em role.
            RoleClaimType = "role"
        };

        // JWT via query string para SignalR (WebSocket não envia header Authorization).
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    ctx.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Policy "Verified": exige verificação dupla (e-mail + telefone)
    options.AddPolicy("Verified", policy => policy
        .RequireClaim("email_verified", "true")
        .RequireClaim("phone_verified", "true"));

    // Policy "Admin": claim email em Admin:Emails (appsettings). Em dev, Rodney = admin.
    // Em produção, migrar para role/contrato (TODO).
    var adminEmails = builder.Configuration.GetSection("Admin:Emails").Get<string[]>() ?? Array.Empty<string>();
    if (adminEmails.Length > 0)
    {
        options.AddPolicy("Admin", policy => policy.RequireClaim("email", adminEmails));
    }
    else
    {
        // Sem admins configurados: policy sempre nega (mais seguro que permitir tudo).
        options.AddPolicy("Admin", policy => policy.RequireUserName("__no_admin_configured__"));
    }

    // Policy "Arbitrator": resolve disputas de escrow (move fundos). Role ARBITRATOR dedicada
    // (UserRole do Identity, claim role no JWT) OU Admin — admins são árbitros por padrão.
    options.AddPolicy("Arbitrator", policy => policy.RequireRole("Arbitrator", "Admin"));
});

// Forwarded headers (por trás de Traefik + Cloudflared)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Escuta 0.0.0.0:8000 (ASPNETCORE_URLS tem precedência — usado em testes/Docker)
builder.WebHost.UseUrls("http://0.0.0.0:8000");

var app = builder.Build();

app.UseForwardedHeaders();

// Log de request HTTP condensado pelo Serilog (1 evento por request, com método/path/status/duração).
app.UseSerilogRequestLogging();

// Middleware de timeouts (aplica-se apenas a endpoints com [RequestTimeout]). Após ForwardedHeaders.
app.UseRequestTimeouts();

// Tratamento global de exceções: erros de domínio/validação/concorrência viram HTTP limpo
// (400/409) em vez de 500 genérico — UX consistente em todos os endpoints.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (FluentValidation.ValidationException ex)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "Verifique os campos informados.",
            errors = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }),
        });
    }
    catch (Revoa.Abstractions.DomainException ex)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
    catch (Revoa.Abstractions.ConcurrencyException)
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "Conflito de concorrência: o registro mudou desde a última leitura. Recarregue e tente novamente.",
        });
    }
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<CommunityHub>("/hubs/community");
app.MapHub<NotificationsHub>("/hubs/notifications");

app.MapGet("/health", () => Results.Ok(new { status = "healthy", ts = DateTime.UtcNow }));

// Cria índices únicos (Email, Telefone no Identity; UserId no Account) — anti-sybil em nível de banco (idempotente).
    await EnsureIndexesAsync(app);

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // Falha fatal de startup/configuração: garante log + shutdown limpo.
    Log.Fatal(ex, "Host Revoa.Api terminou inesperadamente.");
}
finally
{
    Log.CloseAndFlush();
}

return;

// Configura OpenTelemetry tracing (AspNetCore + HttpClient + MongoDB) + exporter por config.
// Tudo em try/catch: falha de exporter/instrumentação NÃO derruba o app (logue e continue).
static void AddObservability(WebApplicationBuilder builder)
{
    var serviceName = builder.Configuration["Otel:ServiceName"] ?? "revoa-api";
    var exporter = (builder.Configuration["Otel:Exporter"] ?? "none").Trim().ToLowerInvariant();

    try
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName, serviceVersion: "1.0.0"))
            .WithTracing(t =>
            {
                t.AddSource("Revoa")
                 .AddAspNetCoreInstrumentation()
                 .AddHttpClientInstrumentation();

                // MongoDB driver (jbogard DiagnosticSources): nome do ActivitySource varia por versão do pacote.
                // Assinar fonte inexistente é no-op, então cobrimos ambos (plural atual / singular legado).
                t.AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSources");
                t.AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSource");

                switch (exporter)
                {
                    case "console":
                        t.AddConsoleExporter();
                        break;
                    case "otlp":
                    {
                        var endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
                        if (string.IsNullOrWhiteSpace(endpoint))
                        {
                            Log.Warning("OTel: Exporter=otlp, mas OTEL_EXPORTER_OTLP_ENDPOINT ausente — usando default do exporter.");
                        }
                        t.AddOtlpExporter(o =>
                        {
                            if (!string.IsNullOrWhiteSpace(endpoint))
                            {
                                o.Endpoint = new Uri(endpoint);
                            }
                        });
                        break;
                    }
                    default:
                        // "none" (ou desconhecido): tracing sem exporter (spans não são exportados).
                        break;
                }
            });
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Falha ao configurar OpenTelemetry; tracing desativado (app segue sem observabilidade de trace).");
    }
}

// Cria IMongoClient com instrumentação de tracing (spans por comando/conn). Fallback resiliente:
// se o bridge de instrumentação falhar, retorna um cliente plain (sem tracing) em vez de quebrar.
static IMongoClient CreateMongoClient(string connectionString)
{
    var settings = MongoClientSettings.FromConnectionString(connectionString);
    try
    {
        settings.ClusterConfigurator = cb => cb.Subscribe(
            new MongoDB.Driver.Core.Extensions.DiagnosticSources.DiagnosticsActivityEventSubscriber());
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Não foi possível plugar instrumentação MongoDB; cliente sem tracing.");
    }
    return new MongoClient(settings);
}

static async Task EnsureIndexesAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    // Índices por coleção via IMongoIndexEnsurer — cada módulo registra seus repositórios
    // assim no DI. Falha em um ensurer não aborta os demais nem o startup (Mongo
    // indisponível em dev → só loga).
    foreach (var ensurer in scope.ServiceProvider.GetServices<IMongoIndexEnsurer>())
    {
        try
        {
            await ensurer.EnsureIndexesAsync();
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Falha ao criar índices de {Ensurer} (Mongo indisponível?).",
                ensurer.GetType().Name);
        }
    }

    // Seed de categorias padrão (idempotente) após os índices — dev/onboarding.
    if (scope.ServiceProvider.GetRequiredService<Revoa.Catalog.Domain.Repositories.ICategoryRepository>()
        is CategoriesRepository categoriesRepo)
    {
        try
        {
            await categoriesRepo.EnsureSeedAsync();
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Não foi possível semear as categorias padrão.");
        }
    }
}

// Expõe o entry point minimal para WebApplicationFactory<Program> (testes de integração).
// Partial: o gerador de top-level statements cria a outra metade; nada funcional muda em runtime.
public partial class Program { }
