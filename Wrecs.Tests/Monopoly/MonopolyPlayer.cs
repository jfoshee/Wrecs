namespace Wrecs.Tests.Monopoly;

public record MonopolyPlayer(string Name) : IMonopolyEntity
{
    public int Id { get; } = EntityId.Next();
}
