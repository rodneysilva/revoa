using Revoa.Identity.Domain.Aggregates.UserAggregate;

namespace Revoa.Identity.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<User?> GetByEmailAsync(string email, CancellationToken ct);

    Task<User?> GetByPhoneAsync(string phone, CancellationToken ct);

    // Painel admin: todos os usuários, mais recentes primeiro, cap 500.
    Task<IReadOnlyList<User>> GetAllAsync(int limit, CancellationToken ct);

    Task AddAsync(User user, CancellationToken ct);

    Task UpdateAsync(User user, CancellationToken ct);
}
