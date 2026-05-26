using System;
using System.Collections.Generic;

namespace Wrecs.Core;

public class AgentContext : IAgentContext
{
    private readonly Dictionary<Type, IStateSnapshot> _snapshots = [];
    private readonly Dictionary<Type, object> _bags = [];

    public void AddSnapshot<T>(T snapshot) where T : IStateSnapshot
    {
        _snapshots[typeof(T)] = snapshot;
    }

    public void Add<T>(T item) where T : notnull
    {
        _bags[typeof(T)] = item;
    }

    public bool HasSnapshot<T>() where T : IStateSnapshot => _snapshots.ContainsKey(typeof(T));
    public bool Has<T>() => _bags.ContainsKey(typeof(T));

    public T GetSnapshot<T>() where T : IStateSnapshot
    {
        if (_snapshots.TryGetValue(typeof(T), out var snapshot))
            return (T)snapshot;
        return default!;
    }

    public T Get<T>()
    {
        if (_bags.TryGetValue(typeof(T), out var item))
            return (T)item;
        return default!;
    }
}