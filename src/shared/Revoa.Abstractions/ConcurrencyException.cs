namespace Revoa.Abstractions;

public class ConcurrencyException : Exception
{
    public string EntityId { get; }
    public long ExpectedVersion { get; }

    public ConcurrencyException(string entityId, long expectedVersion)
        : base($"Conflito de concorrência na entidade '{entityId}' (versão esperada {expectedVersion}).")
    {
        EntityId = entityId;
        ExpectedVersion = expectedVersion;
    }
}
