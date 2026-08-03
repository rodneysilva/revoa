namespace Revoa.Abstractions;

public abstract class Entity : IEntity
{
    public Guid Id { get; protected set; }
    public long Version { get; protected set; }

    public void IncrementVersion() => Version++;
}
