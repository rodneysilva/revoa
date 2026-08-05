using Revoa.Identity.Application.Services;

namespace Revoa.IntegrationTests.Harness;

// Stub de IEmailSender: o MailKitEmailSender real tentaria SMTP (127.0.0.1:25 em dev) e lançaria
// exceção sem Postfix, quebrando o registro (500). Em testes de integração o token de verificação
// é obtido via /api/auth/dev-verify (DEV-ONLY), então o envio de e-mail pode ser silenciado.
public sealed class NoopEmailSender : IEmailSender
{
    public Task SendVerificationEmailAsync(string toEmail, string token, CancellationToken ct)
        => Task.CompletedTask;

    public Task SendLoginCodeAsync(string toEmail, string code, CancellationToken ct)
        => Task.CompletedTask;
}
