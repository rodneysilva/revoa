using Revoa.Abstractions;

namespace Revoa.Admin.Domain.Aggregates.SystemParameterAggregate;

// Parâmetro de sistema configurável em runtime (UF-30). Um documento por Key (coleção
// SystemParameters, índice único por Key). ValueJson guarda o valor serializado em JSON
// (string/number/bool); cada módulo desserializa no tipo que lhe interessa via IParameterStore.
// UpdatedBy = e-mail do admin autenticado (claim). Bump de Version é responsabilidade do
// repositório (upsert por Key) — NUNCA do aggregate. SetValue apenas atualiza os campos
// (ValueJson/UpdatedBy/UpdatedAt); NÃO chama IncrementVersion (regra do projeto).
public class SystemParameter : AggregateRoot
{
    public string Key { get; private set; } = string.Empty;

    // Valor serializado em JSON (raw do tipo armazenado: "5", "1.5", "true", "\"x\"").
    public string ValueJson { get; private set; } = string.Empty;

    public string UpdatedBy { get; private set; } = string.Empty;

    public DateTime UpdatedAt { get; private set; }

    private SystemParameter() { }

    // Factory: valida Key/UpdatedBy. NÃO IncrementVersion (o repo faz Upsert).
    public static SystemParameter Create(string key, string valueJson, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException("Key do parâmetro é obrigatória.");
        }

        if (string.IsNullOrWhiteSpace(updatedBy))
        {
            throw new DomainException("UpdatedBy é obrigatório.");
        }

        return new SystemParameter
        {
            Id = Guid.NewGuid(),
            Key = key,
            ValueJson = valueJson ?? string.Empty,
            UpdatedBy = updatedBy,
            UpdatedAt = DateTime.UtcNow,
            Version = 1
        };
    }

    // Atualiza o valor + auditoria. NÃO IncrementVersion (o repo faz o bump no upsert).
    public void SetValue(string valueJson, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(updatedBy))
        {
            throw new DomainException("UpdatedBy é obrigatório.");
        }

        ValueJson = valueJson ?? string.Empty;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }
}
