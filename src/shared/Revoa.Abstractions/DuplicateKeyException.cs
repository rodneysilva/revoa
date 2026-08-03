namespace Revoa.Abstractions;

public class DuplicateKeyException : Exception
{
    public DuplicateKeyException(string message) : base(message)
    {
    }
}
