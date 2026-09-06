namespace Revoa.Infrastructure.Persistence;

/// <summary>
/// Marcador de repositório que cria índices no startup. O Program.cs percorre todos os
/// registrados no DI (um por coleção) — módulo novo só precisa registrar o repo como
/// IMongoIndexEnsurer, sem tocar na composição da API.
/// </summary>
public interface IMongoIndexEnsurer
{
    Task EnsureIndexesAsync(CancellationToken ct = default);
}
