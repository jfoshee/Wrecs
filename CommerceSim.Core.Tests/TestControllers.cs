namespace CommerceSim.Core.Tests;

/// <summary>
/// A controller that adds interest to the money balance of all agents.
/// </summary>
class InterestController(double interestRate) : ICommercialController
{
    public IEnumerable<IEntity> GetEntitiesToUpdate(IEnumerable<IEntity> allEntities) => allEntities;

    public CommercialSnapshot GetNewState(IEntity entity, CommercialSnapshot currentState)
    {
        var interest = (int)(currentState.MoneyBalance * interestRate);
        return currentState with { MoneyBalance = currentState.MoneyBalance + interest };
    }
}

/// <summary>
/// A controller that grants resources to a specific entity each tick (e.g., simulating mining).
/// </summary>
class MiningController(IEntity miner, int resourcesPerTick) : ICommercialController
{
    public IEnumerable<IEntity> GetEntitiesToUpdate(IEnumerable<IEntity> allEntities)
    {
        return allEntities.Where(e => e == miner);
    }

    public CommercialSnapshot GetNewState(IEntity entity, CommercialSnapshot currentState)
    {
        var newResourceBalance = currentState.ResourceBalance + resourcesPerTick;
        return new CommercialSnapshot(currentState.MoneyBalance, newResourceBalance);
    }
}
