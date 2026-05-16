namespace Wrecs.Tests;

/// <summary>
/// A controller that adds interest to the money balance of all agents.
/// </summary>
class InterestController(double interestRate) : IController<MoneySnapshot>
{
    public IEnumerable<IEntity> GetEntitiesToUpdate(IEnumerable<IEntity> allEntities) => allEntities;

    public MoneySnapshot GetNewState(IEntity entity, MoneySnapshot currentState)
    {
        var interest = (int)(currentState.MoneyBalance * interestRate);
        return new MoneySnapshot(currentState.MoneyBalance + interest);
    }
}

/// <summary>
/// A controller that grants unitless resources to a specific entity each tick (e.g., simulating mining).
/// </summary>
class MiningController(IEntity miner, int resourcesPerTick) : IController<InventorySnapshot>
{
    public IEnumerable<IEntity> GetEntitiesToUpdate(IEnumerable<IEntity> allEntities) =>
        allEntities.Where(e => e == miner);

    public InventorySnapshot GetNewState(IEntity entity, InventorySnapshot currentState)
    {
        var newAmount = currentState.GetAmount("") + resourcesPerTick;
        var updated = currentState.Inventory
            .Where(i => i.Type != "")
            .Append(("", newAmount));
        return new InventorySnapshot(updated);
    }
}
