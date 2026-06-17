namespace Wrecs.Tests;

/// <summary>
/// A controller that adds interest to the money balance of all agents.
/// </summary>
class InterestController(double interestRate) : IPrepareSharedUpdates, IRequire<MoneySystem>
{
    private MoneySystem? _moneySystem;
    public void Inject(MoneySystem system) => _moneySystem = system;

    public IEnumerable<UpdateSet> PrepareSharedUpdates()
    {
        var updates = _moneySystem!.GetEntities().Select(entity =>
        {
            var balance = _moneySystem.GetTypedState(entity).MoneyBalance;
            var interest = (int)(balance * interestRate);
            return (IEntityUpdate)new MoneyUpdate(entity, balance + interest);
        });
        yield return new(updates);
    }
}

/// <summary>
/// A controller that grants unitless resources to a specific entity each tick (e.g., simulating mining).
/// </summary>
class MiningController(IEntity miner, int resourcesPerTick) : IPrepareSharedUpdates, IRequire<InventorySystem>
{
    private InventorySystem? _inventorySystem;
    public void Inject(InventorySystem system) => _inventorySystem = system;

    public IEnumerable<UpdateSet> PrepareSharedUpdates()
    {
        var current = _inventorySystem!.GetTypedState(miner);
        var newAmount = current.GetAmount("") + resourcesPerTick;
        var updated = current.Inventory
            .Where(i => i.Type != "")
            .Append(("", newAmount));
        yield return new([new InventoryUpdate(miner, [.. updated])]);
    }
}
