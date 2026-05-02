namespace CommerceSim.Core;

/// <summary>
/// Takes a snapshot of all state at every tick
/// </summary>
public class LoggingSim(Sim sim) : ISystem
{
    private readonly List<IReadOnlyDictionary<int, CommercialSnapshot>> _snapshots = [];

    public void ApplyController(IController controller, IEnumerable<ISystem> matchingSystems) { }
    public bool MatchesController(IController controller) => false;
    public void ApplyStateUpdates(IController controller, IEntity[] entities) { }
    public void InitEntities(IEnumerable<(IEntity entity, IStateSnapshot[] initialStates)> entitiesWithState) { }

    public IReadOnlyList<IReadOnlyDictionary<int, CommercialSnapshot>> GetSnapshots() => _snapshots;

    public void Tick()
    {
        sim.Tick();
        var snapshot = sim.GetStateSnapshot();
        _snapshots.Add(snapshot);
    }
}
