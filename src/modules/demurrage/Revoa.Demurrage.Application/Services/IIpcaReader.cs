namespace Revoa.Demurrage.Application.Services;

// Porta: leitura do IPCA (série SGS 433 do BCB — variação % mensal) para o reajuste
// trimestral da taxa de demurrage. Sem ele a taxa deflaciona em termos reais e o custo de
// manter RVM ocioso vai caindo — o oposto do objetivo do demurrage.
public interface IIpcaReader
{
    /// <summary>
    /// Variação % acumulada dos últimos <paramref name="months"/> meses. Indisponível → null
    /// (o chamador apenas adia o reajuste — nunca falha o fluxo por causa do IPCA).
    /// </summary>
    Task<double?> GetAccumulatedAsync(int months, CancellationToken ct = default);
}
