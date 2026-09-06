using MongoDB.Driver;
using Revoa.Abstractions;
using Revoa.Identity.Domain.Aggregates.UserAggregate;
using Revoa.Identity.Domain.Repositories;
using Revoa.Infrastructure.Persistence;

namespace Revoa.Identity.Infrastructure.Persistence;

public class UsersRepository : MongoRepositoryBase<User>, IUserRepository, IMongoIndexEnsurer
{
    public UsersRepository(IMongoDatabase database) : base(database, "Users")
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        return await Collection.Find(u => u.Email == email).FirstOrDefaultAsync(ct);
    }

    public async Task<User?> GetByPhoneAsync(string phone, CancellationToken ct)
    {
        return await Collection.Find(u => u.Telefone == phone).FirstOrDefaultAsync(ct);
    }

    // Traduz violação de índice único (Email/Telefone) em exceção de domínio — mantém o
    // módulo Application livre de dependência do MongoDB (anti-sybil em nível de banco).
    public override async Task AddAsync(User user, CancellationToken ct = default)
    {
        try
        {
            await Collection.InsertOneAsync(user, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new DuplicateKeyException("Já existe um usuário com esse e-mail ou telefone.");
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

        await Collection.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<User>(emailKeys,
                new CreateIndexOptions { Name = "ux_Email", Unique = true }),
            new CreateIndexModel<User>(phoneKeys,
                new CreateIndexOptions { Name = "ux_Telefone", Unique = true })
        }, ct);
    }
}
