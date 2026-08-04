using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Revoa.Catalog.Application.Services;

namespace Revoa.Catalog.Infrastructure.Services;

// Consulta ViaCEP (https://viacep.com.br/ws/{cep}/json/). Lat/lng vem do front (HTML5);
// aqui só enriquecemos bairro/cidade por CEP.
public class ViaCepService : IViaCepService
{
    private readonly HttpClient _http;
    private readonly ILogger<ViaCepService> _logger;

    public ViaCepService(HttpClient http, ILogger<ViaCepService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<ViaCepAddress?> GetByCepAsync(string cep, CancellationToken ct = default)
    {
        var clean = new string(cep.Where(char.IsDigit).ToArray());
        if (clean.Length != 8)
        {
            return null;
        }

        try
        {
            var resp = await _http.GetFromJsonAsync<ViaCepResponse>(
                $"https://viacep.com.br/ws/{clean}/json/", ct);

            if (resp is null || resp.Erro)
            {
                return null;
            }

            return new ViaCepAddress(resp.Bairro, resp.Localidade, resp.Uf);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ViaCEP indisponível para cep {Cep}.", clean);
            return null;
        }
    }

    private sealed class ViaCepResponse
    {
        public bool Erro { get; set; }
        public string? Bairro { get; set; }
        public string? Localidade { get; set; } // cidade
        public string? Uf { get; set; }         // estado
    }
}
