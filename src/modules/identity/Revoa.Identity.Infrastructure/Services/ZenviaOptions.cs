namespace Revoa.Identity.Infrastructure.Services;

public class ZenviaOptions
{
    public const string SectionName = "Zenvia";

    public string? ApiToken { get; set; }
    public string? From { get; set; }

    /// <summary>
    /// Apenas para ambiente de DESENVOLVIMENTO: loga o OTP (para testar o fluxo de verificação
    /// localmente). NUNCA ativar em produção (vazaria o código de verificação).
    /// </summary>
    public bool LogOtpInDev { get; set; }
}
