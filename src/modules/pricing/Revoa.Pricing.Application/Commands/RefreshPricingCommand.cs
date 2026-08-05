using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Revoa.Abstractions;
using Revoa.IntegrationContracts.Admin;
using Revoa.IntegrationContracts.Pricing;
using Revoa.Pricing.Application.Options;
using Revoa.Pricing.Domain.Aggregates.PriceReferenceAggregate;
using Revoa.Pricing.Domain.Repositories;

namespace Revoa.Pricing.Application.Commands;

// Refresh das referências de preço justo por categoria (UF-28, admin). Recalcula a mediana
// comunitária RVM por categoria, combina com BRL admin-seed + IPCA IBGE, e refina a sugestão
// justa via Ollama (opcional, fallback mediana). Resiliente: falhas de IBGE/Ollama são logadas
// e seguem (SourcesUsed reflete o que deu certo). Retorna o número de categorias atualizadas.
//
// BrlRate e UseOllama vêm do IParameterStore (runtime, UF-30) com fallback para os defaults do
// PricingOptions — store vazio => comportamento atual. Os demais parâmetros (BrlReferences,
// OllamaUrl/Model, IbgeUrl) seguem do IOptions.
public sealed record RefreshPricingCommand : IRequest<Result<int>>;

public class RefreshPricingCommandHandler : IRequestHandler<RefreshPricingCommand, Result<int>>
{
    private readonly IListingPriceReader _reader;
    private readonly IPriceReferenceRepository _repo;
    private readonly PricingOptions _options;
    private readonly IParameterStore _parameters;
    private readonly HttpClient _http;
    private readonly ILogger<RefreshPricingCommandHandler> _logger;

    public RefreshPricingCommandHandler(
        IListingPriceReader reader,
        IPriceReferenceRepository repo,
        IOptions<PricingOptions> options,
        IParameterStore parameters,
        HttpClient http,
        ILogger<RefreshPricingCommandHandler> logger)
    {
        _reader = reader;
        _repo = repo;
        _options = options.Value;
        _parameters = parameters;
        _http = http;
        _logger = logger;
    }

    public async Task<Result<int>> Handle(RefreshPricingCommand request, CancellationToken ct)
    {
        var samples = await _reader.GetActiveAsync(ct);
        if (samples.Count == 0)
        {
            _logger.LogInformation("Pricing refresh: nenhum anúncio ativo com preço — 0 categorias.");
            return Result<int>.Ok(0);
        }

        // Parâmetros runtime (BrlRate/UseOllama) com fallback para os defaults do IOptions.
        // GetAsync<T> (T sem constraint) colapsa T? para o próprio tipo em value types.
        var brlRate = await _parameters.GetAsync("Pricing.BrlRate", _options.BrlRate, ct);
        var useOllama = await _parameters.GetAsync("Pricing.UseOllama", _options.UseOllama, ct);

        // IPCA é buscado UMA vez por refresh (cache local).
        var (ipcRate, ipcMonth) = await FetchIpcAsync(ct);
        if (ipcRate is null)
        {
            _logger.LogWarning("Pricing refresh: IPCA IBGE indisponível — seguindo sem ele.");
        }

        var byCategory = samples
            .GroupBy(s => s.CategoriaId)
            .ToList();

        var updated = 0;
        foreach (var group in byCategory)
        {
            var prices = group.Select(s => s.PrecoRvm).Where(p => p > 0).ToArray();
            if (prices.Length == 0)
            {
                continue; // skip categoria sem amostras válidas.
            }

            var categoriaId = group.Key;
            var slug = group.Select(s => s.CategoriaSlug).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
            var median = Median(prices);

            // BRL: rate global efetiva; referência absoluta por slug (se houver seed).
            long? brlReference = null;
            if (_options.BrlReferences is not null
                && !string.IsNullOrWhiteSpace(slug)
                && _options.BrlReferences.TryGetValue(slug, out var seed))
            {
                brlReference = (long)Math.Round(seed);
            }

            var sources = new List<string> { "community" };
            if (ipcRate is not null)
            {
                sources.Add("ipca");
            }

            // Sugestão justa: default = mediana (robusto). Ollama refina se responder um número válido.
            var fair = median;
            if (useOllama)
            {
                var aiFair = await AskFairAsync(slug ?? categoriaId.ToString(), median, brlRate, ipcRate, ct);
                if (aiFair is { } ai && ai > 0)
                {
                    fair = ai;
                    sources.Add("ollama");
                }
                else
                {
                    _logger.LogDebug("Pricing refresh: Ollama sem resposta válida p/ {Slug} — usando mediana.", slug);
                }
            }

            var reference = PriceReference.Create(
                categoriaId,
                slug,
                median,
                prices.Length,
                brlRate,
                brlReference,
                fair,
                ipcRate,
                ipcMonth,
                string.Join(",", sources));

            await _repo.UpsertAsync(reference, ct);
            updated++;
        }

        _logger.LogInformation("Pricing refresh concluído: {Count} categorias atualizadas.", updated);
        return Result<int>.Ok(updated);
    }

    // Mediana: ordena, pega o central (ou média dos 2 centrais).
    private static long Median(long[] values)
    {
        Array.Sort(values);
        var mid = values.Length / 2;
        if (values.Length % 2 == 1)
        {
            return values[mid];
        }
        return (values[mid - 1] + values[mid]) / 2;
    }

    // IPCA IBGE: últimos 12 meses, variação mensal (%). Retorna o mês mais recente (rate%, "YYYYMM").
    // Resiliente: qualquer falha → (null, null).
    private async Task<(decimal? rate, string? month)> FetchIpcAsync(CancellationToken ct)
    {
        try
        {
            using var resp = await _http.GetAsync(_options.IbgeUrl, ct);
            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            // root[0].resultados[0].series[0].serie = { "YYYYMM": "0.42", ... }
            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
            {
                return (null, null);
            }

            var serie = doc.RootElement[0]
                .GetProperty("resultados")[0]
                .GetProperty("series")[0]
                .GetProperty("serie");

            string? lastMonth = null;
            decimal? lastRate = null;
            foreach (var prop in serie.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String
                    && decimal.TryParse(prop.Value.GetString(), out var v))
                {
                    lastMonth = prop.Name;
                    lastRate = v;
                }
            }

            return (lastRate, lastMonth);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Pricing refresh: erro ao buscar IPCA IBGE.");
            return (null, null);
        }
    }

    // Ollama: pede UM número inteiro (sugestão justa em RVM). Parse robusto: primeiro \d+.
    // Resiliente: qualquer falha → null (fallback mediana). Timeout de ~30s configurado no HttpClient.
    private async Task<long?> AskFairAsync(
        string categoriaKey, long median, decimal brlRate, decimal? ipcRate, CancellationToken ct)
    {
        try
        {
            var prompt =
                $"Você é um assistente de precificação justa para uma economia circular (moeda social RVM). " +
                $"Categoria: {categoriaKey}. Preço mediano atual dos anúncios ativos: {median} RVM. " +
                $"Taxa de referência BRL (1 RVM ≈ R${brlRate}). " +
                $"IPCA último mês: {(ipcRate.HasValue ? $"{ipcRate.Value}%" : "n/d")}. " +
                $"Sugira um preço justo em RVM. Responda apenas com o número inteiro.";

            var body = JsonSerializer.Serialize(new
            {
                model = _options.OllamaModel,
                prompt,
                stream = false
            });

            using var content = new StringContent(body, Encoding.UTF8);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using var resp = await _http.PostAsync($"{_options.OllamaUrl.TrimEnd('/')}/api/generate", content, ct);
            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var responseText = doc.RootElement.GetProperty("response").GetString();
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return null;
            }

            var match = Regex.Match(responseText, @"\d+");
            return match.Success && long.TryParse(match.Value, out var n) ? n : null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Pricing refresh: Ollama indisponível p/ {Key}.", categoriaKey);
            return null;
        }
    }
}
