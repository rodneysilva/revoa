namespace Revoa.Application.Media;

// Porta de armazenamento de mídia (imagens de anúncio etc.). Implementada pelo
// Infrastructure (MinIO). O retorno é a URL pública RELATIVA (/api/media/{key})
// — a API serve o objeto por stream, mantendo o bucket privado (mesma origem
// da SPA, funciona em dev.revoa.me sem expor o MinIO).
public interface IMediaStore
{
    // Salva o conteúdo e devolve a URL relativa para embutir na entidade.
    Task<string> SaveAsync(
        string prefix,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken ct = default);

    // Abre o objeto pela chave; null se não existir.
    Task<(Stream Content, string ContentType)?> OpenAsync(string key, CancellationToken ct = default);
}
