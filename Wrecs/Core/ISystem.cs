namespace Wrecs.Core;

public interface ISystem
{
    /// <summary>
    /// Applies any internal updates to this System's own state for this Tick.
    /// </summary>
    void ApplyInternalUpdates();
}

public interface IHasEntities
{
    void InitEntities(IEnumerable<(IEntity entity, IStateSnapshot[] initialStates)> entitiesWithState);
    IReadOnlyList<IEntity> GetEntities();
}

public interface IHasEntityState
{
    IStateSnapshot GetState(IEntity entity);
}

public interface IPrepareInternalUpdates
{
    /// <summary>
    /// Using the current state of the world, prepares any internal updates
    /// that this System will perform on its own state during this Tick.
    /// </summary>
    void PrepareInternalUpdates();
}

public interface ISystem<TMarkerInterface, TStateSnapshot> : ISystem, IHasEntities, IHasEntityState, IPrepareInternalUpdates
    where TMarkerInterface : IEntity
    where TStateSnapshot : struct, IStateSnapshot
{
    void InitEntities(params (IEntity entity, TStateSnapshot? initialState)[] initialEntities);
    TStateSnapshot GetTypedState(IEntity entity);

    // Default interface method to apply generic types
    void IHasEntities.InitEntities(IEnumerable<(IEntity entity, IStateSnapshot[] initialStates)> entitiesWithState)
    {
        // An entity is relevant to this system if it implements the marker interface or has an initial state for this system
        var matchingEntities = entitiesWithState
            .Select(e => (e.entity, initialState:
                e.initialStates.OfType<TStateSnapshot>().Select(s => (TStateSnapshot?)s).FirstOrDefault()))
            .Where(e => e.entity is TMarkerInterface || e.initialState != null)
            .ToArray();

        InitEntities(matchingEntities);
    }

    IStateSnapshot IHasEntityState.GetState(IEntity entity) => (IStateSnapshot)GetTypedState(entity);
}

/// <summary>
/// A System that can accept updates from external sources
/// </summary>
public interface IAcceptUpdates : ISystem
{
    void ApplyUpdates(IEnumerable<IEntityUpdate> updates);
}

public interface IAcceptUpdates<TStateSnapshot> : IAcceptUpdates
    where TStateSnapshot : IStateSnapshot
{
    void ApplyUpdates(IEnumerable<EntityUpdate<TStateSnapshot>> updates);

    void IAcceptUpdates.ApplyUpdates(IEnumerable<IEntityUpdate> updates)
    {
        var typedUpdates = updates.OfType<EntityUpdate<TStateSnapshot>>();
        ApplyUpdates(typedUpdates);
    }
}
