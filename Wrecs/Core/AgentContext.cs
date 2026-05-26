namespace Wrecs.Core;

public class AgentContext : IAgentContext
{
    private readonly Dictionary<Type, IStateSnapshot> _snapshots = [];

    public void AddSnapshot<T>(T snapshot) where T : IStateSnapshot
    {
        _snapshots[typeof(T)] = snapshot;
    }

    public bool HasSnapshot<T>() where T : IStateSnapshot => _snapshots.ContainsKey(typeof(T));

    public T GetSnapshot<T>() where T : IStateSnapshot
    {
        if (_snapshots.TryGetValue(typeof(T), out var snapshot))
            return (T)snapshot;
        return default!;
    }
}
