namespace Revoa.Identity.Application.Services;

public interface IEmailSender
{
    Task SendVerificationEmailAsync(string toEmail, string token, CancellationToken ct);
}
