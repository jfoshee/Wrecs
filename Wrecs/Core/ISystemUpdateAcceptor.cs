namespace Wrecs.Core;

/// <summary>
/// A System that can accept updates from external sources
/// </summary>
public interface ISystemUpdateAcceptor : ISystem
{
    void ApplyUpdates(IEnumerable<IEntityUpdate> updates);
}

public interface ISystemUpdateAcceptor<TStateSnapshot> : ISystemUpdateAcceptor
    where TStateSnapshot : IStateSnapshot
{
    void ApplyUpdates(IEnumerable<EntityUpdate<TStateSnapshot>> updates);

    void ISystemUpdateAcceptor.ApplyUpdates(IEnumerable<IEntityUpdate> updates)
    {
        var typedUpdates = updates.OfType<EntityUpdate<TStateSnapshot>>();
        ApplyUpdates(typedUpdates);
    }
}
