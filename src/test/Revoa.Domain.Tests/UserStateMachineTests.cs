using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Revoa.Abstractions;
using Revoa.Identity.Domain.Aggregates.UserAggregate;
using Xunit;

namespace Revoa.Domain.Tests;

// Ciclo de vida do User: cadastro → dupla verificação (e-mail+telefone) → Active,
// banimento, roles e login passwordless. OTPs travam após 5 tentativas (OtpMaxAttempts).
public class UserStateMachineTests
{
    private static User NewUser() => User.Create("Marina Costa", "marina@revoa.dev", "+5511900000001", idadeOk: true);

    // O domínio não faz cripto: recebe o hash pronto (mesma forma do HmacOtpHasher de
    // produção — HMAC-SHA256 sobre chave derivada por HKDF). Chave de teste fixa.
    private static string Hash(string otp) => Convert.ToHexString(HMACSHA256.HashData(
        HKDF.DeriveKey(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes("test-key-user-state-machine"), 32,
            Encoding.UTF8.GetBytes("revoa.otp.v1"), info: null),
        Encoding.UTF8.GetBytes(otp)));

    // --- Cadastro ---

    [Fact]
    public void Create_MenorDe18_LancaDomainException()
    {
        var act = () => User.Create("João", "joao@revoa.dev", "+5511900000002", idadeOk: false);
        act.Should().Throw<DomainException>().WithMessage("*18*");
    }

    [Fact]
    public void Create_Valido_NascePendenteComRoleUser()
    {
        var user = NewUser();

        user.Status.Should().Be(UserStatus.PendingVerification);
        user.Role.Should().Be(UserRole.User);
        user.EmailVerified.Should().BeFalse();
        user.PhoneVerified.Should().BeFalse();
        user.Version.Should().Be(1);
    }

    // --- Verificação de e-mail ---

    [Fact]
    public void VerifyEmail_TokenCorretoEAoVivo_VerificaELimpaToken()
    {
        var user = NewUser();
        user.SetEmailVerification("tok-123", DateTime.UtcNow.AddHours(1));

        var ok = user.VerifyEmail("tok-123");

        ok.Should().BeTrue();
        user.EmailVerified.Should().BeTrue();
        user.EmailToken.Should().BeNull();
        user.EmailTokenExpiry.Should().BeNull();
    }

    [Fact]
    public void VerifyEmail_TokenErrado_RetornaFalseESemEfeito()
    {
        var user = NewUser();
        user.SetEmailVerification("tok-123", DateTime.UtcNow.AddHours(1));

        user.VerifyEmail("errado").Should().BeFalse();

        user.EmailVerified.Should().BeFalse();
        user.Status.Should().Be(UserStatus.PendingVerification);
    }

    [Fact]
    public void VerifyEmail_TokenExpirado_RetornaFalse()
    {
        var user = NewUser();
        user.SetEmailVerification("tok-123", DateTime.UtcNow.AddHours(-1));

        user.VerifyEmail("tok-123").Should().BeFalse();
        user.EmailVerified.Should().BeFalse();
    }

    [Fact]
    public void VerifyEmail_JaVerificado_RetornaTrueIdempotente()
    {
        var user = NewUser();
        user.SetEmailVerification("tok-123", DateTime.UtcNow.AddHours(1));
        user.VerifyEmail("tok-123");

        user.VerifyEmail("qualquer").Should().BeTrue("verificação já concluída");
    }

    // --- Verificação de telefone (OTP + lockout 5 tentativas) ---

    [Fact]
    public void VerifyPhone_OtpCorreto_VerificaELimpaHash()
    {
        var user = NewUser();
        user.SetPhoneVerification(Hash("654321"), DateTime.UtcNow.AddMinutes(10));

        var ok = user.VerifyPhone(Hash("654321"));

        ok.Should().BeTrue();
        user.PhoneVerified.Should().BeTrue();
        user.PhoneOtpHash.Should().BeNull();
    }

    [Fact]
    public void VerifyPhone_OtpExpirado_RetornaFalse()
    {
        var user = NewUser();
        user.SetPhoneVerification(Hash("654321"), DateTime.UtcNow.AddMinutes(-1));

        user.VerifyPhone(Hash("654321")).Should().BeFalse();
    }

    [Fact]
    public void VerifyPhone_CincoErros_BloqueiaAteOCorreto()
    {
        var user = NewUser();
        user.SetPhoneVerification(Hash("654321"), DateTime.UtcNow.AddMinutes(10));

        for (var i = 0; i < 5; i++)
        {
            user.VerifyPhone(Hash("000000")).Should().BeFalse($"tentativa {i + 1}");
        }

        // Bloqueado: nem o código certo passa mais.
        user.VerifyPhone(Hash("654321")).Should().BeFalse("OTP bloqueado após 5 tentativas");
        user.PhoneVerified.Should().BeFalse();
    }

    [Fact]
    public void VerifyPhone_ErrosContamMasNaoBloqueiamAntesDoLimite()
    {
        var user = NewUser();
        user.SetPhoneVerification(Hash("654321"), DateTime.UtcNow.AddMinutes(10));

        user.VerifyPhone(Hash("000000")).Should().BeFalse();
        user.VerifyPhone(Hash("654321")).Should().BeTrue("4 tentativas restantes ainda");
    }

    // --- Ativação (dupla verificação) ---

    [Fact]
    public void SoEmailVerificado_MantemPendingVerification()
    {
        var user = NewUser();
        user.SetEmailVerification("tok", DateTime.UtcNow.AddHours(1));
        user.VerifyEmail("tok");

        user.Status.Should().Be(UserStatus.PendingVerification);
    }

    [Fact]
    public void EmailETelefoneVerificados_AtivaUsuario()
    {
        var user = NewUser();
        user.SetEmailVerification("tok", DateTime.UtcNow.AddHours(1));
        user.SetPhoneVerification(Hash("654321"), DateTime.UtcNow.AddMinutes(10));
        user.VerifyEmail("tok");
        user.VerifyPhone(Hash("654321"));

        user.Status.Should().Be(UserStatus.Active);
    }

    // --- Ban / role ---

    [Fact]
    public void Ban_VaiParaBanned()
    {
        var user = NewUser();
        user.DevActivate();

        user.Ban();

        user.Status.Should().Be(UserStatus.Banned);
    }

    [Fact]
    public void SetRole_AtribuiSemMudarStatus()
    {
        var user = NewUser();
        var statusAntes = user.Status;

        user.SetRole(UserRole.Arbitrator);

        user.Role.Should().Be(UserRole.Arbitrator);
        user.Status.Should().Be(statusAntes, "role não muda status");
    }

    // --- Login passwordless (código por e-mail) ---

    [Fact]
    public void VerifyLoginCode_CodigoCorreto_SucessoEUsoUnico()
    {
        var user = NewUser();
        user.SetLoginCode(Hash("999888"), DateTime.UtcNow.AddMinutes(10));

        user.VerifyLoginCode(Hash("999888")).Should().BeTrue();
        user.LoginCodeHash.Should().BeNull();

        // Uso único: segunda chamada falha (hash consumido).
        user.VerifyLoginCode(Hash("999888")).Should().BeFalse();
    }

    [Fact]
    public void VerifyLoginCode_Expirado_RetornaFalse()
    {
        var user = NewUser();
        user.SetLoginCode(Hash("999888"), DateTime.UtcNow.AddMinutes(-1));

        user.VerifyLoginCode(Hash("999888")).Should().BeFalse();
    }

    [Fact]
    public void VerifyLoginCode_CincoErros_Bloqueia()
    {
        var user = NewUser();
        user.SetLoginCode(Hash("999888"), DateTime.UtcNow.AddMinutes(10));

        for (var i = 0; i < 5; i++)
        {
            user.VerifyLoginCode(Hash("000000")).Should().BeFalse();
        }

        user.VerifyLoginCode(Hash("999888")).Should().BeFalse("código bloqueado após 5 tentativas");
    }

    // --- DevActivate (bypass DEV-only do /api/auth/dev-verify) ---

    [Fact]
    public void DevActivate_MarcaDuplaVerificacaoEAtiva()
    {
        var user = NewUser();

        user.DevActivate();

        user.Status.Should().Be(UserStatus.Active);
        user.EmailVerified.Should().BeTrue();
        user.PhoneVerified.Should().BeTrue();
        user.EmailToken.Should().BeNull();
        user.PhoneOtpHash.Should().BeNull();
    }
}
