namespace CommerceSim.Core;

public class LogTickSystem(IOutput output) : ISystem
{
    private int _tickCount = 0;

    public void ApplyController(IController controller, IEnumerable<ISystem> matchingSystems) { }
    public bool MatchesController(IController controller) => false;
    public void ApplyStateUpdates(IController controller, IEntity[] entities) { }
    public void InitEntities(IEnumerable<(IEntity entity, IStateSnapshot[] initialStates)> entitiesWithState) { }

    public void Tick()
    {
        output.WriteLine($"=== Tick {_tickCount} ===");
        _tickCount++;
    }
}
