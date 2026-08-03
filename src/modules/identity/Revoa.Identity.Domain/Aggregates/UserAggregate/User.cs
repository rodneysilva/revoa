using Revoa.Abstractions;

namespace Revoa.Identity.Domain.Aggregates.UserAggregate;

public enum UserRole
{
    User,
    Mod,
    Admin
}

public enum UserStatus
{
    Active,
    Banned,
    Inactive
}

public class User : AggregateRoot
{
    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Telefone { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IdadeOk { get; private set; }
    public UserStatus Status { get; private set; }
    public bool EmailVerified { get; private set; }
    public bool PhoneVerified { get; private set; }

    private User()
    {
    }

    public static User Create(string nome, string email, string telefone, bool idadeOk)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Email = email,
            Telefone = telefone,
            Role = UserRole.User,
            IdadeOk = idadeOk,
            Status = UserStatus.Active,
            EmailVerified = false,
            PhoneVerified = false,
            Version = 1
        };
    }

    public void MarkEmailVerified() => EmailVerified = true;

    public void MarkPhoneVerified() => PhoneVerified = true;

    public void Ban() => Status = UserStatus.Banned;
}
