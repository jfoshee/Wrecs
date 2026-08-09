namespace Wrecs.Core;

/// <summary>
/// A System that has Entities. It should be initialized with Entity state and can
/// provide a snapshot of Entity state.
/// </summary>
public interface ISystemWithEntities : ISystemEntityStateInitializer, ISystemEntityStateProvider;

public interface ISystemWithEntityStateSnapshots
{
    bool HasInitialState(IEnumerable<IStateSnapshot> initialStates);
}

public interface ISystemWithEntityStateSnapshots<TStateSnapshot>
    : ISystemWithEntityStateSnapshots, ISystemEntityStateProvider
    where TStateSnapshot : struct, IStateSnapshot
{
    TStateSnapshot GetTypedState(IEntity entity);

    bool ISystemWithEntityStateSnapshots.HasInitialState(IEnumerable<IStateSnapshot> initialStates) =>
        initialStates.Any(initialState => initialState is TStateSnapshot);

    IStateSnapshot ISystemEntityStateProvider.GetState(IEntity entity) => GetTypedState(entity);
}

public interface ISystemWithEntityMarker
{
    bool IsMarkedEntity(IEntity entity);
}

public interface ISystemWithEntityMarker<TMarkerInterface>
    : ISystemWithEntityMarker
    where TMarkerInterface : IEntity
{
    bool ISystemWithEntityMarker.IsMarkedEntity(IEntity entity) => entity is TMarkerInterface;
}

/// <inheritdoc/>
public interface ISystemWithEntities<TMarkerInterface, TStateSnapshot> :
    ISystemWithEntities,
    ISystemWithEntityMarker<TMarkerInterface>,
    ISystemWithEntityStateSnapshots<TStateSnapshot>
    where TMarkerInterface : IEntity
    where TStateSnapshot : struct, IStateSnapshot
{
    void InitEntities(params (IEntity entity, TStateSnapshot? initialState)[] initialEntities);

    // Default interface method to adapt the untyped initialization API to the typed one.
    // Sim is responsible for passing only the entities that concern this system.
    void ISystemEntityStateInitializer.InitEntities(IEnumerable<(IEntity entity, IStateSnapshot[] initialStates)> entitiesWithState)
    {
        var initialEntities = entitiesWithState
            .Select(e => (e.entity, initialState:
                e.initialStates.OfType<TStateSnapshot>().Select(s => (TStateSnapshot?)s).FirstOrDefault()))
            .ToArray();

        InitEntities(initialEntities);
    }
}

/// <summary>
/// A system with per-entity state that explicitly supports adding entities after initialization.
/// </summary>
public interface ISystemWithDynamicEntities<TMarkerInterface, TStateSnapshot> :
    ISystemWithEntities<TMarkerInterface, TStateSnapshot>,
    ISystemEntityStateAdder<TStateSnapshot>
    where TMarkerInterface : IEntity
    where TStateSnapshot : struct, IStateSnapshot;
