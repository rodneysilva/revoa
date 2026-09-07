using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Revoa.Abstractions;
using Revoa.Application.Media;

namespace Revoa.Api.Controllers;

// Mídia (upload/serve de imagens). Upload exige login e grava em
// listings/{userId}/…; o download é público (o anúncio define a visibilidade)
// e sai por stream do MinIO — o bucket nunca é exposto diretamente.
[ApiController]
[Route("api/media")]
public class MediaController : ControllerBase
{
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];

    private readonly IMediaStore _media;

    public MediaController(IMediaStore media)
    {
        _media = media;
    }

    // Upload de imagem (multipart). Retorna { Url } relativa para o anúncio.
    // Query folder: allowlist "communities" → communities/{userId}/… (capa);
    // qualquer outro valor (ou ausente) → listings/{userId}/… (retrocompatível).
    // RequestSizeLimit = arquivo + envelope multipart (senão 413 antes do 400 amigável).
    [HttpPost]
    [Authorize]
    [RequestSizeLimit(MaxBytes + 64 * 1024)]
    public async Task<IActionResult> Upload(
        IFormFile file, [FromQuery] string? folder, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ApiError("Arquivo vazio."));
        }

        if (file.Length > MaxBytes)
        {
            return BadRequest(new ApiError("Imagem acima de 5 MB."));
        }

        var contentType = file.ContentType?.ToLowerInvariant() ?? "";
        if (!AllowedContentTypes.Contains(contentType))
        {
            return BadRequest(new ApiError("Envie uma imagem JPEG, PNG, WebP ou GIF."));
        }

        var user = User.GetRevoaUser();
        if (user is null)
        {
            return Unauthorized();
        }

        await using var stream = file.OpenReadStream();
        var prefix = folder == "communities" ? "communities" : "listings";
        var url = await _media.SaveAsync($"{prefix}/{user.UserId}", file.FileName, contentType, stream, ct);

        return Ok(new { Url = url });
    }

    // Serve o objeto por stream (anônimo — a visibilidade é do anúncio).
    [HttpGet("{**key}")]
    [AllowAnonymous]
    [ResponseCache(Duration = 86400 * 30, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Get(string key, CancellationToken ct)
    {
        var opened = await _media.OpenAsync(key, ct);
        if (opened is null)
        {
            return NotFound();
        }

        var (content, contentType) = opened.Value;
        return File(content, string.IsNullOrEmpty(contentType) ? "application/octet-stream" : contentType);
    }
}
