namespace Wrecs.Core;

public sealed record class Entity(string Name) : IEntity
{
    public int Id { get; } = EntityId.Next();
}
