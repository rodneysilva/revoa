using System.Security.Cryptography;
using System.Text;

namespace Revoa.Community.Domain.Aggregates.CommunityAggregate;

// MVP/dev: SHA256 com salt estático. NÃO usar em produção.
// TODO: trocar por PBKDF2/ASP.NET Identity Core (salt aleatório por hash + iterações) em produção.
public static class PasswordHasher
{
    private const string StaticSalt = "revoa-static-salt";

    public static string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password + StaticSalt));
        return Convert.ToHexString(bytes);
    }

    public static bool Verify(string password, string? hash)
        => !string.IsNullOrWhiteSpace(hash) && Hash(password) == hash;
}
