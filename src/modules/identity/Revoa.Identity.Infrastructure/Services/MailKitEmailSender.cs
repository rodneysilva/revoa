using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Revoa.Identity.Application.Services;

namespace Revoa.Identity.Infrastructure.Services;

public class MailKitEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<MailKitEmailSender> _logger;

    public MailKitEmailSender(IOptions<EmailOptions> options, ILogger<MailKitEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(string toEmail, string token, CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_options.From));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Confirme seu e-mail — revoa.me";

        var link = $"https://revoa.me/verify-email?token={token}";
        message.Body = new TextPart("plain")
        {
            Text = $"Bem-vindo ao revoa.me!\n\nConfirme seu e-mail acessando o link:\n{link}"
        };

        using var client = new SmtpClient();
        try
        {
            // TLS oportunístico: usa STARTTLS se o servidor oferecer (produção); em rede interna
            // (Postfix local) cai para plaintext. Evita token de verificação capturável por MITM em prod.
            await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.Auto, ct);
            await AuthenticateAsync(client, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail para {Email} via {Host}:{Port}", toEmail, _options.Host, _options.Port);
            // DEV: mesmo com falha no SMTP, registra o token p/ o fluxo de verificação não travar.
            if (_options.LogVerificationTokenInDev)
            {
                _logger.LogWarning("[MailKit DEV] Token de e-mail {Token} -> {Email} (link: {Link})", token, toEmail, link);
            }
            throw;
        }

        // DEV: SMTP ok, mas em dev o Postfix local não entrega em caixa real — loga o token.
        if (_options.LogVerificationTokenInDev)
        {
            _logger.LogWarning("[MailKit DEV] Token de e-mail {Token} -> {Email} (link: {Link})", token, toEmail, link);
        }
    }

    public async Task SendLoginCodeAsync(string toEmail, string code, CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_options.From));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Seu código de acesso — revoa.me";
        message.Body = new TextPart("plain") { Text = $"Seu código de acesso ao revoa.me é: {code}\n\nEle expira em 10 minutos. Se não foi você, ignore este e-mail." };

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.Auto, ct);
            await AuthenticateAsync(client, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar código de login para {Email} via {Host}:{Port}", toEmail, _options.Host, _options.Port);
            if (_options.LogVerificationTokenInDev)
            {
                _logger.LogWarning("[MailKit DEV] Código de login {Code} -> {Email}", code, toEmail);
            }
            throw;
        }

        if (_options.LogVerificationTokenInDev)
        {
            _logger.LogWarning("[MailKit DEV] Código de login {Code} -> {Email}", code, toEmail);
        }
    }

    // Relay autenticado (ex.: smtp.gmail.com:587). Sem usuário E senha configurados =
    // Postfix interno sem auth (rede interna do compose). Meio-configurado (só usuário)
    // não autentica: falha no próprio SMTP com erro claro, não com senha vazia.
    private async Task AuthenticateAsync(SmtpClient client, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_options.Username) && !string.IsNullOrEmpty(_options.Password))
        {
            await client.AuthenticateAsync(_options.Username, _options.Password, ct);
        }
    }
}
