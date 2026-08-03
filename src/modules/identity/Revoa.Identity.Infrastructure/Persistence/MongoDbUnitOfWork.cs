using MongoDB.Driver;
using Revoa.Identity.Domain.Repositories;

namespace Revoa.Identity.Infrastructure.Persistence;

public class MongoDbUnitOfWork : IUnitOfWork
{
    private readonly IMongoClient _client;

    public MongoDbUnitOfWork(IMongoClient client)
    {
        _client = client;
    }

    // MVP: cada repositório persiste com optimistic locking.
    // SaveChangesAsync é preparado p/ uso futuro em ACID seletiva (finalização financeira).
    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
}
