using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Revoa.Identity.Application.Services;

namespace Revoa.Identity.Infrastructure.Services;

// Envio transacional pela API REST do Brevo (POST /v3/smtp/email, header api-key).
// Escolhido no lugar do SMTP quando Email:ApiKey está setada: a chave v3 NÃO
// autentica no host smtp-relay (Brevo exige chave SMTP separada, 535), e a API
// dispensa MX/PTR/reputação de quem envia. ADR-0014.
public class BrevoApiEmailSender : IEmailSender
{
    private const string ApiUrl = "https://api.brevo.com/v3/smtp/email";

    private readonly EmailOptions _options;
    private readonly HttpClient _http;
    private readonly ILogger<BrevoApiEmailSender> _logger;

    public BrevoApiEmailSender(
        IOptions<EmailOptions> options,
        HttpClient http,
        ILogger<BrevoApiEmailSender> logger)
    {
        _options = options.Value;
        _http = http;
        _logger = logger;
    }

    public Task SendVerificationEmailAsync(string toEmail, string token, CancellationToken ct)
    {
        var link = $"https://revoa.me/verify-email?token={token}";
        return SendAsync(
            toEmail,
            "Confirme seu e-mail — revoa.me",
            $"Bem-vindo ao revoa.me!\n\nConfirme seu e-mail acessando o link:\n{link}",
            () => _logger.LogWarning("[Brevo DEV] Token de e-mail {Token} -> {Email} (link: {Link})", token, toEmail, link),
            ct);
    }

    public Task SendLoginCodeAsync(string toEmail, string code, CancellationToken ct)
    {
        return SendAsync(
            toEmail,
            "Seu código de acesso — revoa.me",
            $"Seu código de acesso ao revoa.me é: {code}\n\nEle expira em 10 minutos. Se não foi você, ignore este e-mail.",
            () => _logger.LogWarning("[Brevo DEV] Código de login {Code} -> {Email}", code, toEmail),
            ct);
    }

    // Corpo e textos idênticos aos do MailKitEmailSender — só o transporte muda.
    private async Task SendAsync(
        string toEmail,
        string subject,
        string text,
        Action logDev,
        CancellationToken ct)
    {
        var payload = new
        {
            sender = new { email = _options.From },
            to = new[] { new { email = toEmail } },
            subject,
            textContent = text,
        };

        using var resp = await _http.PostAsJsonAsync(ApiUrl, payload, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Falha ao enviar e-mail para {Email} via API Brevo: {Status} {Body}",
                toEmail, (int)resp.StatusCode, body);
            throw new InvalidOperationException($"Brevo {(int)resp.StatusCode}: {body}");
        }

        // Paridade com o MailKit: em dev o token/código também vai pro log.
        if (_options.LogVerificationTokenInDev)
        {
            logDev();
        }
    }
}
