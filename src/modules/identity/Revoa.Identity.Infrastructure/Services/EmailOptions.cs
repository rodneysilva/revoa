namespace Revoa.Identity.Infrastructure.Services;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = "mail";
    public int Port { get; set; } = 25;
    public string From { get; set; } = "no-reply@revoa.me";

    // Nome de exibição do remetente (header From: "revoa.me <no-reply@revoa.me>").
    public string FromName { get; set; } = "revoa.me";

    // Relay SMTP autenticado (ex.: smtp.gmail.com:587 com App Password). Vazio = Postfix
    // interno sem auth (rede interna). Necessário p/ ENTREGA real: saída direta de IP
    // residencial não chega aos grandes MX (sem PTR/reputação, porta 25 filtrada).
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";

    // Chave v3 do Brevo (header api-key). Setada = envia pela API REST em vez do
    // SMTP — a chave v3 não autentica no smtp-relay do Brevo (exige chave SMTP).
    public string ApiKey { get; set; } = "";

    // DEV: loga o token/link de verificação no logger (igual ao LogOtpInDev do Zenvia).
    // Em dev o Postfix local NÃO entrega em caixa real — o token só é obtível via log/endpoint dev.
    public bool LogVerificationTokenInDev { get; set; }

    // Chave secreta do HMAC dos OTPs (login/telefone). Vazio = deriva de Jwt:Key
    // (HKDF com domínio próprio). NUNCA persistir junto do hash no banco.
    public string OtpHashKey { get; set; } = "";
}
