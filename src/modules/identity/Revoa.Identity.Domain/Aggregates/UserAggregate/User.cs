using System.Security.Cryptography;
using System.Text;
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
    PendingVerification,
    Active,
    Banned,
    Inactive
}

public class User : AggregateRoot
{
    private const int OtpMaxAttempts = 5;

    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Telefone { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IdadeOk { get; private set; }
    public UserStatus Status { get; private set; }
    public bool EmailVerified { get; private set; }
    public bool PhoneVerified { get; private set; }

    // Verificação de e-mail (token de uso único)
    public string? EmailToken { get; private set; }
    public DateTime? EmailTokenExpiry { get; private set; }

    // Verificação de telefone (OTP hasheado)
    public string? PhoneOtpHash { get; private set; }
    public DateTime? PhoneOtpExpiry { get; private set; }
    public int PhoneOtpAttempts { get; private set; }

    // Login passwordless por código de e-mail (magic OTP). Prova posse do e-mail no login.
    public string? LoginCodeHash { get; private set; }
    public DateTime? LoginCodeExpiry { get; private set; }
    public int LoginCodeAttempts { get; private set; }

    private User()
    {
    }

    public static User Create(string nome, string email, string telefone, bool idadeOk)
    {
        if (!idadeOk)
        {
            throw new DomainException("Idade mínima de 18 anos não atendida.");
        }

        return new User
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Email = email,
            Telefone = telefone,
            Role = UserRole.User,
            IdadeOk = idadeOk,
            Status = UserStatus.PendingVerification,
            EmailVerified = false,
            PhoneVerified = false,
            Version = 1
        };
    }

    public void SetEmailVerification(string token, DateTime expiry)
    {
        EmailToken = token ?? throw new ArgumentNullException(nameof(token));
        EmailTokenExpiry = expiry;
    }

    public void SetPhoneVerification(string otp, DateTime expiry)
    {
        PhoneOtpHash = HashOtp(otp);
        PhoneOtpExpiry = expiry;
        PhoneOtpAttempts = 0;
    }

    public bool VerifyEmail(string token)
    {
        if (EmailVerified)
        {
            return true;
        }

        if (string.IsNullOrEmpty(EmailToken) || !CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(EmailToken), Encoding.UTF8.GetBytes(token ?? string.Empty)))
        {
            return false;
        }

        if (EmailTokenExpiry is null || EmailTokenExpiry < DateTime.UtcNow)
        {
            return false;
        }

        EmailVerified = true;
        EmailToken = null;
        EmailTokenExpiry = null;
        TryActivate();
        return true;
    }

    public bool VerifyPhone(string code)
    {
        if (PhoneVerified)
        {
            return true;
        }

        if (PhoneOtpAttempts >= OtpMaxAttempts)
        {
            return false;
        }

        if (string.IsNullOrEmpty(PhoneOtpHash)
            || PhoneOtpExpiry is null
            || PhoneOtpExpiry < DateTime.UtcNow)
        {
            return false;
        }

        if (!CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(PhoneOtpHash), Encoding.UTF8.GetBytes(HashOtp(code ?? string.Empty))))
        {
            PhoneOtpAttempts++;
            return false;
        }

        PhoneVerified = true;
        PhoneOtpHash = null;
        PhoneOtpExpiry = null;
        PhoneOtpAttempts = 0;
        TryActivate();
        return true;
    }

    private void TryActivate()
    {
        if (EmailVerified && PhoneVerified && Status == UserStatus.PendingVerification)
        {
            Status = UserStatus.Active;
        }
    }

    public void Ban() => Status = UserStatus.Banned;

    // --- Login passwordless por código de e-mail ---
    public void SetLoginCode(string code, DateTime expiry)
    {
        LoginCodeHash = HashOtp(code);
        LoginCodeExpiry = expiry;
        LoginCodeAttempts = 0;
    }

    public bool VerifyLoginCode(string code)
    {
        if (LoginCodeAttempts >= OtpMaxAttempts)
        {
            return false;
        }

        if (string.IsNullOrEmpty(LoginCodeHash)
            || LoginCodeExpiry is null
            || LoginCodeExpiry < DateTime.UtcNow)
        {
            return false;
        }

        if (!CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(LoginCodeHash), Encoding.UTF8.GetBytes(HashOtp(code ?? string.Empty))))
        {
            LoginCodeAttempts++;
            return false;
        }

        LoginCodeHash = null;
        LoginCodeExpiry = null;
        LoginCodeAttempts = 0;
        return true;
    }

    // DEV-ONLY: ativa o usuário sem token/OTP (bypass da verificação dupla). Usado apenas pelo
    // endpoint /api/auth/dev-verify (gated IsDevelopment). NUNCA em produção. Marca verificado,
    // limpa tokens e ativa — equivalente a ter confirmado e-mail + telefone.
    public void DevActivate()
    {
        EmailVerified = true;
        PhoneVerified = true;
        EmailToken = null;
        EmailTokenExpiry = null;
        PhoneOtpHash = null;
        PhoneOtpExpiry = null;
        PhoneOtpAttempts = 0;
        TryActivate();
        IncrementVersion();
    }

    private static string HashOtp(string otp) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(otp)));
}
