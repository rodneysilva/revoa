using System.Text.Json;
using Revoa.Admin.Domain.Aggregates.SystemParameterAggregate;
using Revoa.Admin.Domain.Repositories;
using Revoa.IntegrationContracts.Admin;

namespace Revoa.Admin.Infrastructure;

// Implementação da porta IParameterStore (UF-30). Persiste/lê parâmetros runtime no Mongo via
// ISystemParameterRepository, serializando os valores em JSON. Resiliente: GetAsync NUNCA lança —
// se a chave não existir ou o JSON não desserializar no tipo pedido, devolve o defaultValue.
// Assim, com o store vazio, o comportamento é idêntico ao atual (fallback para IOptions/appsettings).
//
// Observação sobre JsonElement: quando o SetParameterCommand grava um JsonElement recebido do
// body, JsonSerializer.Serialize(JsonElement) reproduz o JSON bruto (ex.: "5" / "1.5" / "true"),
// preservando o tipo para a desserialização posterior de quem lê (long/decimal/bool).
public class ParameterStore : IParameterStore
{
    private readonly ISystemParameterRepository _repo;

    public ParameterStore(ISystemParameterRepository repo)
    {
        _repo = repo;
    }

    public async Task<T?> GetAsync<T>(string key, T? defaultValue = default, CancellationToken ct = default)
    {
        var parameter = await _repo.GetByKeyAsync(key, ct);
        if (parameter is null || string.IsNullOrWhiteSpace(parameter.ValueJson))
        {
            return defaultValue;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(parameter.ValueJson);
        }
        catch (JsonException)
        {
            // JSON corrompido/incompatível com o tipo pedido -> fallback seguro (nunca lança).
            return defaultValue;
        }
    }

    public async Task SetAsync<T>(string key, T value, string updatedBy, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value);

        var existing = await _repo.GetByKeyAsync(key, ct);
        if (existing is null)
        {
            await _repo.UpsertAsync(SystemParameter.Create(key, json, updatedBy), ct);
            return;
        }

        // Atualiza campos + auditoria; o bump de Version acontece no Upsert (regra do projeto).
        existing.SetValue(json, updatedBy);
        await _repo.UpsertAsync(existing, ct);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken ct = default)
    {
        var all = await _repo.GetAllAsync(ct);
        return all.ToDictionary(p => p.Key, p => p.ValueJson);
    }
}
