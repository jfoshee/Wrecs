namespace CommerceSim.Core;

public class LogTickSystem(IOutput output) : ISystem
{
    private int _tickCount = 0;

    public void InitEntities(IEnumerable<(IEntity entity, IStateSnapshot[] initialStates)> entitiesWithState) { }

    public void Tick()
    {
        output.WriteLine($"=== Tick {_tickCount} ===");
        _tickCount++;
    }
}
