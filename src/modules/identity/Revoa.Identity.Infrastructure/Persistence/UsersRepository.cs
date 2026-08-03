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

    public async Task<User?> GetByPhoneAsync(string phone, CancellationToken ct)
    {
        return await _users.Find(u => u.Telefone == phone).FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(User user, CancellationToken ct)
    {
        try
        {
            await _users.InsertOneAsync(user, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // Traduz violação de índice único (Email/Telefone) em exceção de domínio — mantém o
            // módulo Application livre de dependência do MongoDB (anti-sybil em nível de banco).
            throw new DuplicateKeyException("Já existe um usuário com esse e-mail ou telefone.");
        }
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

    /// <summary>
    /// Cria índices únicos em Email e Telefone (idempotente). Garante unicidade anti-sybil em nível
    /// de banco (elimina a TOCTOU do check-then-insert). Chamar no startup da aplicação.
    /// </summary>
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        var emailKeys = Builders<User>.IndexKeys.Ascending(u => u.Email);
        var phoneKeys = Builders<User>.IndexKeys.Ascending(u => u.Telefone);

        await _users.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<User>(emailKeys,
                new CreateIndexOptions { Name = "ux_Email", Unique = true }),
            new CreateIndexModel<User>(phoneKeys,
                new CreateIndexOptions { Name = "ux_Telefone", Unique = true })
        }, ct);
    }
}
