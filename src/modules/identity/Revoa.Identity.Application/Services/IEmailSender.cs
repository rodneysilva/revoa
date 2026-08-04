namespace Revoa.Identity.Application.Services;

public interface IEmailSender
{
    Task SendVerificationEmailAsync(string toEmail, string token, CancellationToken ct);

    // Código de login passwordless (prova posse do e-mail). Em prod chega na caixa; em dev é logado.
    Task SendLoginCodeAsync(string toEmail, string code, CancellationToken ct);
}
