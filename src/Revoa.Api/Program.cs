using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Revoa.Abstractions;
using Revoa.Api.Hubs;
using Revoa.Identity.Infrastructure;
using Revoa.Identity.Infrastructure.Persistence;
using Revoa.Infrastructure;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddOpenApi();

// Event bus de integração in-process (MVP)
builder.Services.AddScoped<IIntegrationEventBus, InProcessIntegrationEventBus>();

// Identity module: Mongo, repo, UoW, e-mail (MailKit), SMS (Zenvia), MediatR+ValidationBehavior, validators
builder.Services.AddIdentityInfrastructure(builder.Configuration);

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
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = "sub"
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Policy "Verified": exige verificação dupla (e-mail + telefone)
    options.AddPolicy("Verified", policy => policy
        .RequireClaim("email_verified", "true")
        .RequireClaim("phone_verified", "true"));
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<CommunityHub>("/hubs/community");

app.MapGet("/health", () => Results.Ok(new { status = "healthy", ts = DateTime.UtcNow }));

// Cria índices únicos (Email, Telefone) no MongoDB — anti-sybil em nível de banco (idempotente).
await EnsureIdentityIndexesAsync(app);

app.Run();
return;

static async Task EnsureIdentityIndexesAsync(WebApplication app)
{
    try
    {
        using var scope = app.Services.CreateScope();
        if (scope.ServiceProvider.GetRequiredService<Revoa.Identity.Domain.Repositories.IUserRepository>()
            is UsersRepository concrete)
        {
            await concrete.EnsureIndexesAsync();
        }
    }
    catch (Exception ex)
    {
        // Não derrubar o startup se o Mongo estiver indisponível em dev; loga e segue.
        app.Logger.LogWarning(ex, "Não foi possível criar índices do Identity no MongoDB (Mongo indisponível?).");
    }
}
