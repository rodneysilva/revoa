using MongoDB.Driver;
using Revoa.Abstractions;
using Revoa.Identity.Domain.Aggregates.UserAggregate;
using Revoa.Identity.Domain.Repositories;

namespace Revoa.Identity.Infrastructure.Persistence;

public class UsersRepository : IUserRepository
{
    private readonly IMongoCollection<User> _users;

    public UsersRepository(IMongoDatabase database)
    {
        _users = database.GetCollection<User>("Users");
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _users.Find(u => u.Id == id).FirstOrDefaultAsync(ct);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        return await _users.Find(u => u.Email == email).FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(User user, CancellationToken ct)
    {
        await _users.InsertOneAsync(user, cancellationToken: ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct)
    {
        var expectedVersion = user.Version;

        // Optimistic locking: _id + (Version == esperada OU doc legado sem Version)
        var filter = Builders<User>.Filter.Eq(u => u.Id, user.Id)
                     & (Builders<User>.Filter.Eq(u => u.Version, expectedVersion)
                        | Builders<User>.Filter.Exists(u => u.Version, false));

        user.IncrementVersion();

        var result = await _users.ReplaceOneAsync(filter, user, cancellationToken: ct);
        if (result.MatchedCount == 0)
        {
            throw new ConcurrencyException(user.Id.ToString(), expectedVersion);
        }
    }
}
