namespace CommerceSim.Core;

public class LogTickSystem(IOutput output) : ISystem, ITickPhases
{
    private int _tickCount = 0;

    public void InitEntities(IEnumerable<(IEntity entity, IStateSnapshot[] initialStates)> entitiesWithState) { }

    public void PrepareUpdates()
    {
        output.WriteLine($"=== Tick {_tickCount} ===");
    }

    public void ApplyUpdates()
    {
        _tickCount++;
    }

    public void Tick()
    {
        PrepareUpdates();
        ApplyUpdates();
    }
}
