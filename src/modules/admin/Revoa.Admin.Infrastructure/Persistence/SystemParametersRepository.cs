using MongoDB.Driver;
using Revoa.Admin.Domain.Aggregates.SystemParameterAggregate;
using Revoa.Admin.Domain.Repositories;

namespace Revoa.Admin.Infrastructure.Persistence;

// Repositório do SystemParameter (coleção própria: SystemParameters). Upsert por Key
// (IsUpsert=true): cria se não existe, substitui se existe. Bump de Version é aqui
// (repositório), nunca no aggregate — a unicidade de Key é o controle deste aggregate.
public class SystemParametersRepository : ISystemParameterRepository
{
    private readonly IMongoCollection<SystemParameter> _parameters;

    public SystemParametersRepository(IMongoDatabase database)
    {
        _parameters = database.GetCollection<SystemParameter>("SystemParameters");
    }

    public async Task<SystemParameter?> GetByKeyAsync(string key, CancellationToken ct)
    {
        return await _parameters.Find(p => p.Key == key).FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<SystemParameter>> GetAllAsync(CancellationToken ct)
    {
        return await _parameters.Find(_ => true).ToListAsync(ct);
    }

    // Upsert por Key (IsUpsert=true): cria se não existe, substitui se existe.
    // Bump de Version é aqui (repositório), nunca no aggregate.
    public async Task UpsertAsync(SystemParameter parameter, CancellationToken ct)
    {
        parameter.IncrementVersion();

        var filter = Builders<SystemParameter>.Filter.Eq(p => p.Key, parameter.Key);
        await _parameters.ReplaceOneAsync(
            filter, parameter, new ReplaceOptions { IsUpsert = true }, ct);
    }

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await _parameters.Indexes.CreateOneAsync(
            new CreateIndexModel<SystemParameter>(
                Builders<SystemParameter>.IndexKeys.Ascending(p => p.Key),
                new CreateIndexOptions { Name = "ux_Key", Unique = true }),
            cancellationToken: ct);
    }
}
