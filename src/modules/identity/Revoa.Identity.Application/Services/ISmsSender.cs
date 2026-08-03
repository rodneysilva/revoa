namespace Revoa.Identity.Application.Services;

public interface ISmsSender
{
    Task SendOtpAsync(string phone, string code, CancellationToken ct);
}
