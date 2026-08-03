namespace Revoa.Identity.Infrastructure.Services;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = "mail";
    public int Port { get; set; } = 25;
    public string From { get; set; } = "no-reply@revoa.me";
}
