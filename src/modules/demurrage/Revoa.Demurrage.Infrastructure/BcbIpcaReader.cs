using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Revoa.Demurrage.Application.Services;

namespace Revoa.Demurrage.Infrastructure;

// IPCA pela API pública do BCB (série SGS 433 — variação % mensal do IPCA).
// "ultimos/N" devolve os N pontos mais recentes PUBLICADOS (o último mês pode ainda não
// estar lá — o BCB divulga perto do dia 10). Acumulado é COMPOSTO (Π(1+iₘ) − 1), igual à
// metodologia do acumulado oficial — não soma simples.
//
// Contrato resiliente: qualquer falha (rede, shape, indisponibilidade) → null + warning;
// o reajuste apenas adia para o próximo trimestre.
public class BcbIpcaReader : IIpcaReader
{
    private readonly HttpClient _http;
    private readonly ILogger<BcbIpcaReader> _logger;

    public BcbIpcaReader(HttpClient http, ILogger<BcbIpcaReader> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<double?> GetAccumulatedAsync(int months, CancellationToken ct = default)
    {
        try
        {
            var url = $"https://api.bcb.gov.br/dados/serie/bcdata.sgs.433/dados/ultimos/{months}?formato=json";
            var points = await _http.GetFromJsonAsync<List<BcbSgsPoint>>(url, ct);
            if (points is null || points.Count == 0)
            {
                _logger.LogWarning("IPCA (BCB 433): resposta vazia — reajuste adiado.");
                return null;
            }

            var compounded = points.Aggregate(1.0, (acc, p) => acc * (1 + p.Valor / 100.0));
            return Math.Round((compounded - 1.0) * 100.0, 4);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "IPCA (BCB 433) indisponível — reajuste trimestral adiado.");
            return null;
        }
    }

    // Shape da série SGS: [{"data":"01/08/2026","valor":0.19}, …]. GetFromJsonAsync usa os
    // web defaults (case-insensitive), então Data/Valor casam com data/valor do BCB.
    private sealed class BcbSgsPoint
    {
        public string Data { get; set; } = string.Empty;
        public double Valor { get; set; }
    }
}
