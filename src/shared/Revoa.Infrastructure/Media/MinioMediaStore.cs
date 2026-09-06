using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;
using Revoa.Application.Media;

namespace Revoa.Infrastructure.Media;

// Minio:Endpoint/AccessKey/SecretKey/Bucket (compose injeta; IntegrationTests
// sobe um container Testcontainers e sobrescreve via UseSetting).
public sealed class MinioOptions
{
    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = "revoa";
    public string SecretKey { get; set; } = "minio123dev";
    public string Bucket { get; set; } = "revoa-media";
    public bool Secure { get; set; }
}

// Adapter MinIO de IMediaStore. Singleton com bucket ensured UMA vez (lazy no
// primeiro uso — nada de I/O no startup). Chaves são opacas (prefixo + GUID),
// jamais derivadas de input do usuário além do prefixo controlado pelo caller.
public sealed class MinioMediaStore : IMediaStore
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    private readonly MinioClient _client;
    private readonly string _bucket;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public MinioMediaStore(IOptions<MinioOptions> options)
    {
        var o = options.Value;
        _client = new MinioClient()
            .WithEndpoint(o.Endpoint)
            .WithCredentials(o.AccessKey, o.SecretKey)
            .WithSSL(o.Secure)
            .Build();
        _bucket = o.Bucket;
    }

    public async Task<string> SaveAsync(
        string prefix,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken ct = default)
    {
        await EnsureBucketAsync(ct);

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
        {
            throw new InvalidOperationException($"Extensão de imagem não suportada: {ext}");
        }

        // prefixo controlado pelo caller (ex.: listings/{userId}); nome = GUID.
        var key = $"{prefix}/{Guid.NewGuid():N}{ext}";
        await _client.PutObjectAsync(
            new PutObjectArgs()
                .Bucket(_bucket)
                .Object(key)
                .StreamData(content)
                .ObjectSize(content.Length)
                .ContentType(contentType),
            ct);

        return $"/api/media/{key}";
    }

    public async Task<(Stream Content, string ContentType)?> OpenAsync(string key, CancellationToken ct = default)
    {
        // Chave opaca: aceita apenas [a-z0-9/-.] e sem ".." — bloqueia path traversal.
        if (string.IsNullOrWhiteSpace(key)
            || key.Contains("..")
            || key.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '/' and not '-' and not '.'))
        {
            return null;
        }

        try
        {
            var stat = await _client.StatObjectAsync(
                new StatObjectArgs().Bucket(_bucket).Object(key), ct);

            var obj = await _client.GetObjectAsync(
                new GetObjectArgs().Bucket(_bucket).Object(key), ct);
            if (obj is null)
            {
                return null;
            }

            return (obj, stat.ContentType);
        }
        catch (ObjectNotFoundException)
        {
            return null;
        }
    }

    private async Task EnsureBucketAsync(CancellationToken ct)
    {
        if (_initialized)
        {
            return;
        }

        await _initLock.WaitAsync(ct);
        try
        {
            if (_initialized)
            {
                return;
            }

            var exists = await _client.BucketExistsAsync(
                new BucketExistsArgs().Bucket(_bucket), ct);
            if (!exists)
            {
                await _client.MakeBucketAsync(
                    new MakeBucketArgs().Bucket(_bucket), ct);
            }

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }
}

public static class MinioMediaStoreRegistration
{
    public static IServiceCollection AddMediaInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<MinioOptions>()
            .Bind(configuration.GetSection("Minio"));
        services.AddSingleton<IMediaStore, MinioMediaStore>();
        return services;
    }
}
