using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Revoa.Notifications.Infrastructure;

// Criptografia Web Push sem dependências externas — só System.Security.Cryptography
// (evita package WebPush/BouncyCastle, que conflita com o Nethereum do módulo token).
// Duas peças:
//   RFC 8291 + RFC 8188 — payload "aes128gcm": ECDH P-256 efêmero + HKDF-SHA256 + AES-128-GCM;
//   RFC 8292 — header Authorization "vapid": JWT ES256 (assinatura r||s crua, 64 bytes).
// Validez garantida pelos vetores oficiais do RFC 8291 §5 (WebPushCryptoTests).
public static class WebPushCrypto
{
    // rs do header aes128gcm: maior que plaintext + delimiter + tag (RFC 8291 §4).
    private const int RecordSize = 4096;

    /// <summary>
    /// Criptografa o payload no content coding aes128gcm (RFC 8291 §3, RFC 8188 §2).
    /// <paramref name="uaPublicKey65"/> = P256dh da inscrição (ponto P-256 não comprimido,
    /// 65 bytes iniciando em 0x04); <paramref name="authSecret"/> = Auth da inscrição (16 bytes).
    /// senderKey/salt injetáveis apenas para reproduzir os vetores do RFC — em produção são
    /// gerados aqui (efêmeros por mensagem).
    /// </summary>
    public static byte[] EncryptPayload(
        byte[] uaPublicKey65,
        byte[] authSecret,
        ReadOnlySpan<byte> plaintext,
        ECDiffieHellman? senderKey = null,
        byte[]? salt = null)
    {
        if (uaPublicKey65.Length != 65 || uaPublicKey65[0] != 0x04)
        {
            throw new ArgumentException(
                "Chave P256dh deve ser o ponto P-256 não comprimido (65 bytes iniciando em 0x04).",
                nameof(uaPublicKey65));
        }

        var ownsSender = senderKey is null;
        var sender = senderKey ?? ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        try
        {
            salt ??= RandomNumberGenerator.GetBytes(16);
            var asPublic = ExportUncompressed(sender.PublicKey);

            // Importa a chave do user agent (o ImportParameters valida o ponto na curva).
            using var ua = ECDiffieHellman.Create();
            ua.ImportParameters(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint { X = uaPublicKey65[1..33], Y = uaPublicKey65[33..65] }
            });
            var ecdhSecret = sender.DeriveRawSecretAgreement(ua.PublicKey);

            // HKDF manual (RFC 8291 §3.4, HMAC-SHA-256 explícito):
            //   PRK_key = HMAC(auth_secret, ecdh); IKM = HMAC(PRK_key, key_info || 0x01)
            //   PRK = HMAC(salt, IKM); CEK/NONCE = HMAC(PRK, info || 0x01)
            var prkKey = HmacSha256(authSecret, ecdhSecret);
            var ikm = HmacSha256(
                prkKey,
                Cat("WebPush: info"u8.ToArray(), [0x00], uaPublicKey65, asPublic, [0x01]));
            var prk = HmacSha256(salt, ikm);
            var cek = HmacSha256(prk, Cat("Content-Encoding: aes128gcm"u8.ToArray(), [0x00, 0x01]))[..16];
            var nonce = HmacSha256(prk, Cat("Content-Encoding: nonce"u8.ToArray(), [0x00, 0x01]))[..12];

            // Header RFC 8188: salt(16) | rs(4, big-endian) | keyid len(1) | keyid = as_public(65).
            // Record único (RFC 8291 §4): plaintext || 0x02, tag GCM de 16 bytes ao final.
            var content = new byte[plaintext.Length + 1];
            plaintext.CopyTo(content);
            content[^1] = 0x02;

            var body = new byte[86 + content.Length + 16];
            salt.CopyTo(body, 0);
            BinaryPrimitives.WriteInt32BigEndian(body.AsSpan(16, 4), RecordSize);
            body[20] = 65;
            asPublic.CopyTo(body, 21);

            using var aes = new AesGcm(cek, 16);
            aes.Encrypt(
                nonce,
                content,
                body.AsSpan(86, content.Length),
                body.AsSpan(86 + content.Length, 16));
            return body;
        }
        finally
        {
            if (ownsSender)
            {
                sender.Dispose();
            }
        }
    }

    /// <summary>
    /// Header Authorization do VAPID (RFC 8292 §3): "vapid t=&lt;JWT ES256&gt;, k=&lt;pub b64url&gt;".
    /// O JWT carrega aud (origin do push service), exp (+12h) e sub (mailto do contato).
    /// <paramref name="privateKeyD"/> = parte privada VAPID (escalar P-256, 32 bytes);
    /// <paramref name="publicKeyB64Url"/> = a MESMA chave pública entregue aos browsers no subscribe.
    /// </summary>
    public static string BuildVapidAuthorization(Uri endpoint, string subject, byte[] privateKeyD, string publicKeyB64Url)
    {
        var audience = endpoint.GetLeftPart(UriPartial.Authority);

        var header = JsonSerializer.SerializeToUtf8Bytes(
            new Dictionary<string, string> { ["typ"] = "JWT", ["alg"] = "ES256" });
        var payload = JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object>
        {
            ["aud"] = audience,
            ["exp"] = DateTimeOffset.UtcNow.AddHours(12).ToUnixTimeSeconds(),
            ["sub"] = subject
        });

        var signingInput = $"{Base64UrlEncode(header)}.{Base64UrlEncode(payload)}";

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportECPrivateKey(ToSec1PrivateKey(privateKeyD), out _);

        // JWS exige assinatura crua (r||s, 64 bytes) — formato P1363 explícito (em .NET 10 é
        // também o default, mas amarrar garante os 64 bytes em qualquer runtime).
        var signature = ecdsa.SignData(
            Encoding.UTF8.GetBytes(signingInput),
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        return $"vapid t={signingInput}.{Base64UrlEncode(signature)}, k={publicKeyB64Url}";
    }

    /// <summary>Ponto público P-256 no form não comprimido (65 bytes, 0x04 || X || Y).</summary>
    public static byte[] ExportUncompressed(ECDiffieHellmanPublicKey publicKey)
    {
        return ToUncompressed(publicKey.ExportParameters().Q);
    }

    /// <summary>Idem, para um ECDsa (chave VAPID: conferir par public/private da env).</summary>
    public static byte[] ExportUncompressedAsP256(ECDsa ecdsa)
    {
        return ToUncompressed(ecdsa.ExportParameters(false).Q);
    }

    private static byte[] ToUncompressed(ECPoint q)
    {
        var point = new byte[65];
        point[0] = 0x04;
        Pad32(q.X, point, 1);
        Pad32(q.Y, point, 33);
        return point;
    }

    /// <summary>Monta SEC1 (ECPrivateKey) só com o escalar d — ImportECPrivateKey deriva o Q.</summary>
    public static byte[] ToSec1PrivateKey(ReadOnlySpan<byte> d)
    {
        var content = new List<byte>(49) { 0x02, 0x01, 0x01, 0x04, (byte)d.Length };
        content.AddRange(d.ToArray());
        content.AddRange(
        [
            0xA0, 0x0A, 0x06, 0x08, // [0] namedCurve
            0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x03, 0x01, 0x07 // OID 1.2.840.10045.3.1.7 (P-256)
        ]);
        return [0x30, (byte)content.Count, .. content];
    }

    // --- DER → r||s removido: .NET 10 assina P1363 direto (overload explícito acima).

    public static string Base64UrlEncode(ReadOnlySpan<byte> value)
    {
        return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static byte[] Base64UrlDecode(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        var padding = (4 - base64.Length % 4) % 4;
        return Convert.FromBase64String(padding == 0 ? base64 : base64 + new string('=', padding));
    }

    private static byte[] HmacSha256(ReadOnlySpan<byte> key, ReadOnlySpan<byte> data)
    {
        using var hmac = new HMACSHA256(key.ToArray());
        return hmac.ComputeHash(data.ToArray());
    }

    private static byte[] Cat(params byte[][] parts)
    {
        var buffer = new byte[parts.Sum(p => p.Length)];
        var offset = 0;
        foreach (var part in parts)
        {
            part.CopyTo(buffer.AsSpan(offset));
            offset += part.Length;
        }

        return buffer;
    }

    private static void Pad32(byte[]? src, byte[] dest, int offset)
    {
        if (src is not null)
        {
            src.CopyTo(dest, offset + (32 - src.Length));
        }
    }
}
