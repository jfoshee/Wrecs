namespace Wrecs.Core;

/// <summary>
/// Base interface for all Systems.
/// </summary>
public interface ISystem;

public interface ISystem<TMarkerInterface, TStateSnapshot> :
    ISystemWithInternalUpdates,
    ISystemWithEntities
    where TMarkerInterface : IEntity
    where TStateSnapshot : struct, IStateSnapshot
{
    void InitEntities(params (IEntity entity, TStateSnapshot? initialState)[] initialEntities);
    TStateSnapshot GetTypedState(IEntity entity);

    // Default interface method to apply generic types
    void ISystemEntityStateInitializer.InitEntities(IEnumerable<(IEntity entity, IStateSnapshot[] initialStates)> entitiesWithState)
    {
        // An entity is relevant to this system if it implements the marker interface or has an initial state for this system
        var matchingEntities = entitiesWithState
            .Select(e => (e.entity, initialState:
                e.initialStates.OfType<TStateSnapshot>().Select(s => (TStateSnapshot?)s).FirstOrDefault()))
            .Where(e => e.entity is TMarkerInterface || e.initialState != null)
            .ToArray();

        InitEntities(matchingEntities);
    }

    IStateSnapshot ISystemEntityStateProvider.GetState(IEntity entity) => (IStateSnapshot)GetTypedState(entity);
}
