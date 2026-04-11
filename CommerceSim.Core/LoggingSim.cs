namespace CommerceSim.Core;

/// <summary>
/// Takes a snapshot of all state at every tick
/// </summary>
public class LoggingSim(CommercialSystem sim) : ISystem
{
    private readonly List<IReadOnlyDictionary<int, AgentStateSnapshot>> _snapshots = [];

    public IReadOnlyList<IReadOnlyDictionary<int, AgentStateSnapshot>> GetSnapshots() => _snapshots;

    public void Tick()
    {
        sim.Tick();
        var snapshot = sim.GetStateSnapshot();
        _snapshots.Add(snapshot);
    }
}
