using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
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

    private readonly IMinioClient _client;
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
                .WithBucket(_bucket)
                .WithObject(key)
                .WithStreamData(content)
                .WithObjectSize(content.Length)
                .WithContentType(contentType),
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
            // Stat primeiro: existência + ContentType. O GetObject entrega o stream via
            // callback — no SDK 7.x o stream só é válido DENTRO do callback, então copia
            // p/ memória (imagens ≤ 5 MB) antes de devolver ao caller.
            var stat = await _client.StatObjectAsync(
                new StatObjectArgs().WithBucket(_bucket).WithObject(key), ct);

            var buffer = new MemoryStream();
            await _client.GetObjectAsync(
                new GetObjectArgs().WithBucket(_bucket).WithObject(key)
                    .WithCallbackStream(s => s.CopyTo(buffer)),
                ct);

            buffer.Position = 0;
            return (buffer, stat.ContentType);
        }
        catch (ObjectNotFoundException)
        {
            return null;
        }
        catch (BucketNotFoundException)
        {
            return null; // bucket ainda não criado = nada publicado
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
                new BucketExistsArgs().WithBucket(_bucket), ct);
            if (!exists)
            {
                await _client.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(_bucket), ct);
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
