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
        if (string.IsNullOrWhiteSpace(_options.ApiToken))
        {
            // Sem token configurado: registra (stub/mock). Não bloqueia o cadastro.
            _logger.LogInformation("[Zenvia STUB] OTP {Code} -> {Phone}", code, phone);
            return;
        }

        // Envio real via Zenvia API (placeholder — corpo do request a confirmar na integração).
        _logger.LogInformation("[Zenvia] enviando OTP para {Phone}", phone);
        await Task.CompletedTask;
    }
}
