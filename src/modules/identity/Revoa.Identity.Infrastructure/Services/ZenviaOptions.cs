namespace Revoa.Identity.Infrastructure.Services;

public class ZenviaOptions
{
    public const string SectionName = "Zenvia";

    public string? ApiToken { get; set; }
    public string? From { get; set; }
}
