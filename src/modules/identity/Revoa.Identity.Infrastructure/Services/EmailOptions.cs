namespace Revoa.Identity.Infrastructure.Services;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = "mail";
    public int Port { get; set; } = 25;
    public string From { get; set; } = "no-reply@revoa.me";

    // DEV: loga o token/link de verificação no logger (igual ao LogOtpInDev do Zenvia).
    // Em dev o Postfix local NÃO entrega em caixa real — o token só é obtível via log/endpoint dev.
    public bool LogVerificationTokenInDev { get; set; }
}
