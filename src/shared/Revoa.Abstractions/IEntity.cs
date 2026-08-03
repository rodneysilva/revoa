namespace Revoa.Abstractions;

public interface IEntity
{
    Guid Id { get; }
    long Version { get; }
}
