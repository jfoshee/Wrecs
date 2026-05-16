using Wrecs.Core;

namespace Wrecs.Systems;

public class LogTickSystem(IOutput output) : ISystem
{
    private int _tickCount = 0;

    public void InitEntities(IEnumerable<(IEntity entity, IStateSnapshot[] initialStates)> entitiesWithState) { }

    public void PrepareInternalUpdates()
    {
        output.WriteLine($"=== Tick {_tickCount} ===");
    }

    public void ApplyInternalUpdates()
    {
        _tickCount++;
    }
}
