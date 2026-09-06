using System.Security.Cryptography;
using System.Text;

namespace Revoa.Community.Domain.Aggregates.CommunityAggregate;

// Hash de senha de acesso a comunidades privadas: PBKDF2 (HMACSHA256, 210k iterações — diretriz
// OWASP), salt aleatório de 16 bytes por hash. Formato: $pbkdf2$v1$<iter>$<saltB64>$<hashB64>.
// Verify aceita também o formato legado (SHA256 + salt estático, hex de 64 chars) dos hashes
// criados antes da migração — UpgradeToPbkdf2 rehasha sob demanda.
public static class PasswordHasher
{
    private const string Prefix = "$pbkdf2$";
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    // Salt estático LEGADO — usado apenas para verificar (e rehashar) hashes antigos.
    private const string LegacyStaticSalt = "revoa-static-salt";

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Prefix}v1${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string? hash)
        => IsLegacy(hash)
            ? VerifyLegacy(password, hash!)
            : VerifyPbkdf2(password, hash);

    // Verdadeiro quando o hash é do formato antigo (SHA256+salt estático): o chamador deve
    // rehashar e persistir (upgrade transparente) após verificar com sucesso.
    public static bool IsLegacy(string? hash)
        => !string.IsNullOrWhiteSpace(hash) && !hash.StartsWith(Prefix, StringComparison.Ordinal);

    private static bool VerifyPbkdf2(string password, string? hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        var parts = hash.Split('$', StringSplitOptions.RemoveEmptyEntries);
        // $pbkdf2$v1$<iter>$<salt>$<hash> → ["pbkdf2", "v1", iter, salt, hash]
        if (parts.Length != 5 || parts[0] != "pbkdf2" || !int.TryParse(parts[2], out var iterations))
        {
            return false;
        }

        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(parts[3]);
            expected = Convert.FromBase64String(parts[4]);
        }
        catch (FormatException)
        {
            return false;
        }

        // iterations do próprio hash (permite eleger custo futuro sem invalidar hashes atuais).
        var actual = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static bool VerifyLegacy(string password, string hash)
    {
        // Hash legado = SHA256 hex (64 chars). Qualquer outra coisa não é ours → falha fechada.
        if (hash.Length != 64)
        {
            return false;
        }

        byte[] expected;
        try
        {
            expected = Convert.FromHexString(hash);
        }
        catch (FormatException)
        {
            return false;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password + LegacyStaticSalt));
        return CryptographicOperations.FixedTimeEquals(expected, bytes);
    }
}
