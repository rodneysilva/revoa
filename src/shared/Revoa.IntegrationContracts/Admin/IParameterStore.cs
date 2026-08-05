namespace Revoa.IntegrationContracts.Admin;

// Porta de leitura/escrita de parâmetros de sistema em RUNTIME (UF-30, Fase 3). Permite que o
// admin altere parametrizações que passam a valer IMEDIATAMENTE, sem reiniciar a aplicação.
// Os valores são persistidos no Mongo (coleção SystemParameters) pelo módulo Admin; os demais
// módulos (Reputation/Demurrage/Pricing) consomem APENAS esta porta — nunca os repositórios do
// Admin — mantendo o isolamento entre bounded contexts (regra: comunicação só via portas/eventos).
//
// Contrato resiliente: GetAsync devolve o defaultValue quando a chave não existe ou o JSON
// armazenado não desserializa no tipo pedido (nunca lança). Assim, com o store vazio, o
// comportamento é idêntico ao atual (fallback para os defaults do IOptions/appsettings).
public interface IParameterStore
{
    // Lê o valor do store; se a chave não existir (ou falhar ao desserializar) -> defaultValue.
    Task<T?> GetAsync<T>(string key, T? defaultValue = default, CancellationToken ct = default);

    // Serializa o valor e grava (upsert por Key). Tende a valer na próxima leitura.
    Task SetAsync<T>(string key, T value, string updatedBy, CancellationToken ct = default);

    // Snapshot de todos os parâmetros (Key -> JSON bruto armazenado).
    Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken ct = default);
}
