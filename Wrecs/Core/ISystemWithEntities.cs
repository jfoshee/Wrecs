namespace Wrecs.Core;

/// <summary>
/// A System that has Entities. It should be initialized with Entity state and can
/// provide a snapshot of Entity state.
/// </summary>
public interface ISystemWithEntities : ISystemEntityStateInitializer, ISystemEntityStateProvider;

/// <inheritdoc/>
public interface ISystemWithEntities<TMarkerInterface, TStateSnapshot> :
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
