using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Revoa.Abstractions;
using Revoa.Api.Hubs;
using Revoa.Identity.Infrastructure;
using Revoa.Infrastructure;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddOpenApi();

// Event bus de integração in-process (MVP)
builder.Services.AddScoped<IIntegrationEventBus, InProcessIntegrationEventBus>();

// Identity module: Mongo, repo, UoW, e-mail (MailKit), SMS (Zenvia), MediatR, validators
builder.Services.AddIdentityInfrastructure(builder.Configuration);

// JWT bearer (esquema; claim sub -> NameIdentifier)
var jwtKey = builder.Configuration["Jwt:Key"]
             ?? "revoa-dev-super-secret-key-change-me-32chars";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "revoa";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = false,
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

app.Run();
