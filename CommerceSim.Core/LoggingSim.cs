namespace CommerceSim.Core;

/// <summary>
/// Takes a snapshot of all state at every tick
/// </summary>
public class LoggingSim(Sim sim) : ISimulator
{
    private readonly List<IReadOnlyDictionary<string, AgentStateSnapshot>> _snapshots = [];

    public IReadOnlyList<IReadOnlyDictionary<string, AgentStateSnapshot>> GetSnapshots() => _snapshots;

    public void Tick()
    {
        sim.Tick();
        var snapshot = sim.GetStateSnapshot();
        _snapshots.Add(snapshot);
    }
}
