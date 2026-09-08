using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Revoa.Notifications.Infrastructure;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Web Push (RFC 8291/8292) validado contra os vetores OFICIAIS do RFC 8291 §5/Appendix A.
// Se um push service um dia recusar o payload, esses testes apontam exatamente qual passo da
// derivação divergiu do padrão. Cripto pura — não tocam Mongo nem HTTP.
public class WebPushCryptoTests
{
    private const string UaPublic =
        "BCVxsr7N_eNgVRqvHtD0zTZsEc6-VV-JvLexhqUzORcxaOzi6-AYWXvTBHm4bjyPjs7Vd8pZGH6SRpkNtoIAiw4";
    private const string UaPrivate = "q1dXpw3UpT5VOmu_cf_v6ih07Aems3njxI-JWgLcM94";
    private const string AsPublic =
        "BP4z9KsN6nGRTbVYI_c7VJSPQTBtkgcy27mlmlMoZIIgDll6e3vCYLocInmYWAmS6TlzAC8wEqKK6PBru3jl7A8";
    private const string AsPrivate = "yfWPiYE-n46HLnH0KqZOF1fJJU3MYrct3AELtAQ-oRw";
    private const string AuthSecret = "BTBZMqHH6r4Tts7J_aSIgg";
    private const string Salt = "DGv6ra1nlYgDCS1FRnbzlw";
    private const string Plaintext = "When I grow up, I want to be a watermelon";
    private const string ExpectedBody =
        "DGv6ra1nlYgDCS1FRnbzlwAAEABBBP4z9KsN6nGRTbVYI_c7VJSPQTBtkgcy27ml"
        + "mlMoZIIgDll6e3vCYLocInmYWAmS6TlzAC8wEqKK6PBru3jl7A_yl95bQpu6cVPT"
        + "pK4Mqgkf1CXztLVBSt2Ks3oZwbuwXPXLWyouBWLVWGNWQexSgSxsj_Qulcy4a-fN";

    [Fact]
    public void Encrypt_matches_rfc8291_official_test_vector()
    {
        using var sender = ECDiffieHellman.Create();
        sender.ImportECPrivateKey(
            WebPushCrypto.ToSec1PrivateKey(WebPushCrypto.Base64UrlDecode(AsPrivate)), out _);

        var body = WebPushCrypto.EncryptPayload(
            WebPushCrypto.Base64UrlDecode(UaPublic),
            WebPushCrypto.Base64UrlDecode(AuthSecret),
            Encoding.UTF8.GetBytes(Plaintext),
            sender,
            WebPushCrypto.Base64UrlDecode(Salt));

        WebPushCrypto.Base64UrlEncode(body).Should().Be(ExpectedBody);
    }

    [Fact]
    public void Ecdh_secret_matches_rfc8291_intermediate_value()
    {
        // Appendix A: ecdh_secret = kyrL1jIIOHEzg3sM2ZWRHDRB62YACZhhSlknJ672kSs. Confere que o
        // DeriveRawSecretAgreement (.NET) produz o mesmo x-coordinate que o RFC espera.
        using var receiver = ECDiffieHellman.Create();
        receiver.ImportECPrivateKey(
            WebPushCrypto.ToSec1PrivateKey(WebPushCrypto.Base64UrlDecode(UaPrivate)), out _);
        using var senderPublic = ImportPublic(WebPushCrypto.Base64UrlDecode(AsPublic));

        var ecdh = receiver.DeriveRawSecretAgreement(senderPublic.PublicKey);

        WebPushCrypto.Base64UrlEncode(ecdh).Should().Be("kyrL1jIIOHEzg3sM2ZWRHDRB62YACZhhSlknJ672kSs");
    }

    [Fact]
    public void Encrypt_roundtrips_on_receiver_side_derivation()
    {
        // Chaves efêmeras + receptor destrinchando o body: header (salt/keyid) → HKDF → GCM,
        // com o delimiter 0x02 no fim do plaintext — exatamente o que o browser faz.
        using var receiver = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var uaPublic = WebPushCrypto.ExportUncompressed(receiver.PublicKey);
        var auth = RandomNumberGenerator.GetBytes(16);
        var plain = Encoding.UTF8.GetBytes("olá, revoa!");

        var body = WebPushCrypto.EncryptPayload(uaPublic, auth, plain);

        body.Length.Should().Be(86 + plain.Length + 1 + 16);

        var salt = body[..16];
        var asPublic = body[21..86];
        using var senderPublic = ImportPublic(asPublic);

        var prkKey = Hmac(auth, receiver.DeriveRawSecretAgreement(senderPublic.PublicKey));
        var ikm = Hmac(prkKey, Cat("WebPush: info"u8.ToArray(), [0x00], uaPublic, asPublic, [0x01]));
        var prk = Hmac(salt, ikm);
        var cek = Hmac(prk, Cat("Content-Encoding: aes128gcm"u8.ToArray(), [0x00, 0x01]))[..16];
        var nonce = Hmac(prk, Cat("Content-Encoding: nonce"u8.ToArray(), [0x00, 0x01]))[..12];

        var cipher = body[86..^16];
        var tag = body[^16..];
        var decrypted = new byte[cipher.Length];
        using (var aes = new AesGcm(cek, 16))
        {
            aes.Decrypt(nonce, cipher, tag, decrypted);
        }

        decrypted.Should().Equal(plain.Append((byte)0x02));
        decrypted[^1].Should().Be(0x02);
    }

    [Fact]
    public void Vapid_authorization_is_es256_jwt_verifiable_with_public_key()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var d = ecdsa.ExportParameters(true).D!;
        var publicB64 = WebPushCrypto.Base64UrlEncode(WebPushCrypto.ExportUncompressedAsP256(ecdsa));

        var authorization = WebPushCrypto.BuildVapidAuthorization(
            new Uri("https://fcm.googleapis.com/fcm/send/abc"), "mailto:contato@revoa.me", d, publicB64);

        const string prefix = "vapid t=";
        authorization.Should().StartWith(prefix);
        authorization.Should().EndWith(", k=" + publicB64);

        var token = authorization[prefix.Length..authorization.LastIndexOf(", k=", StringComparison.Ordinal)];
        var parts = token.Split('.');
        parts.Should().HaveCount(3);

        using var header = JsonDocument.Parse(
            Encoding.UTF8.GetString(WebPushCrypto.Base64UrlDecode(parts[0])));
        header.RootElement.GetProperty("alg").GetString().Should().Be("ES256");

        using var payload = JsonDocument.Parse(
            Encoding.UTF8.GetString(WebPushCrypto.Base64UrlDecode(parts[1])));
        payload.RootElement.GetProperty("aud").GetString().Should().Be("https://fcm.googleapis.com");
        payload.RootElement.GetProperty("sub").GetString().Should().Be("mailto:contato@revoa.me");
        payload.RootElement.GetProperty("exp").GetInt64()
            .Should().BeGreaterThan(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        // Assinatura JWS ES256 = r||s cruas (64 bytes), verificável pela chave pública do k=.
        var signature = WebPushCrypto.Base64UrlDecode(parts[2]);
        signature.Length.Should().Be(64);
        ecdsa.VerifyData(
                Encoding.UTF8.GetBytes(parts[0] + "." + parts[1]),
                signature,
                HashAlgorithmName.SHA256)
            .Should().BeTrue();
    }

    private static ECDiffieHellman ImportPublic(byte[] point65)
    {
        var ecdh = ECDiffieHellman.Create();
        ecdh.ImportParameters(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = point65[1..33], Y = point65[33..65] }
        });
        return ecdh;
    }

    private static byte[] Hmac(byte[] key, byte[] data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(data);
    }

    private static byte[] Cat(params byte[][] parts)
    {
        return parts.SelectMany(p => p).ToArray();
    }
}
