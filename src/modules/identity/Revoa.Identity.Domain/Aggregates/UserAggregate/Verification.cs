namespace Revoa.Identity.Domain.Aggregates.UserAggregate;

public enum VerificationChannel
{
    Email,
    Whatsapp
}

public class Verification
{
    public string Code { get; private set; } = string.Empty;
    public DateTime Expiry { get; private set; }
    public string Target { get; private set; } = string.Empty;
    public VerificationChannel Channel { get; private set; }

    private Verification()
    {
    }

    public Verification(string code, DateTime expiry, string target, VerificationChannel channel)
    {
        Code = code;
        Expiry = expiry;
        Target = target;
        Channel = channel;
    }

    public bool IsValid(string code) => Code == code && DateTime.UtcNow < Expiry;
}
