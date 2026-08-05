using Revoa.Admin.Domain.Aggregates.SystemParameterAggregate;

namespace Revoa.Admin.Domain.Repositories;

// Repositório do agregado SystemParameter (coleção própria: SystemParameters). Upsert por Key
// (IsUpsert=true): cria se não existe, substitui se existe. O bump de Version acontece aqui
// (regra do projeto), nunca no aggregate.
public interface ISystemParameterRepository
{
    Task<SystemParameter?> GetByKeyAsync(string key, CancellationToken ct);

    Task<IReadOnlyList<SystemParameter>> GetAllAsync(CancellationToken ct);

    Task UpsertAsync(SystemParameter parameter, CancellationToken ct);

    Task EnsureIndexesAsync(CancellationToken ct = default);
}
