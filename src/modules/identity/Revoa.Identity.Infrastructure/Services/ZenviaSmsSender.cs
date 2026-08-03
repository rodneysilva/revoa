using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Revoa.Identity.Application.Services;

namespace Revoa.Identity.Infrastructure.Services;

public class ZenviaSmsSender : ISmsSender
{
    private readonly HttpClient _http;
    private readonly ZenviaOptions _options;
    private readonly ILogger<ZenviaSmsSender> _logger;

    public ZenviaSmsSender(HttpClient http, IOptions<ZenviaOptions> options, ILogger<ZenviaSmsSender> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendOtpAsync(string phone, string code, CancellationToken ct)
    {
        // NUNCA logar o OTP em produção (vazaria o código de verificação). Telefone sempre mascarado.
        if (string.IsNullOrWhiteSpace(_options.ApiToken))
        {
            if (_options.LogOtpInDev)
            {
                _logger.LogWarning("[Zenvia STUB/DEV] OTP {Code} -> {Phone} (LogOtpInDev=true; NUNCA usar em produção)", code, MaskPhone(phone));
            }
            else
            {
                _logger.LogInformation("[Zenvia STUB] OTP enviado para {Phone} (código não logado)", MaskPhone(phone));
            }
            return;
        }

        // Envio real via Zenvia API (placeholder — corpo do request a confirmar na integração).
        _logger.LogInformation("[Zenvia] enviando OTP para {Phone}", MaskPhone(phone));
        await Task.CompletedTask;
    }

    private static string MaskPhone(string phone)
    {
        if (string.IsNullOrEmpty(phone) || phone.Length < 4)
        {
            return "***";
        }

        return new string('*', phone.Length - 4) + phone[^4..];
    }
}
