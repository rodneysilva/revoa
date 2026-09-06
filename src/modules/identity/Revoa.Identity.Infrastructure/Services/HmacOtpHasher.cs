using System.Security.Cryptography;
using System.Text;
using Revoa.Identity.Application.Services;

namespace Revoa.Identity.Infrastructure.Services;

// HMAC-SHA256 com chave derivada por HKDF (domínio "revoa.otp.v1"): a chave vem
// de Email:OtpHashKey (ou deriva de Jwt:Key) e NUNCA é persistida — quem lê o
// banco vê o hash mas não consegue validar palpites offline.
public sealed class HmacOtpHasher : IOtpHasher
{
    private readonly byte[] _key;

    public HmacOtpHasher(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("Chave secreta do hash de OTP ausente (Email:OtpHashKey ou Jwt:Key).");
        }

        // Derivação com rótulo próprio: mesmo que o segredo seja o Jwt:Key, a chave
        // efetiva aqui é distinta da chave de assinatura dos tokens.
        _key = HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            Encoding.UTF8.GetBytes(secret),
            32,
            Encoding.UTF8.GetBytes("revoa.otp.v1"),
            info: null);
    }

    public string Hash(string otp) =>
        Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(otp ?? string.Empty)));
}
