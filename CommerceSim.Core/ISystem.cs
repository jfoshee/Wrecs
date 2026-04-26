namespace CommerceSim.Core;

public interface ISystem
{
    void Tick();

    void InitEntities(IEnumerable<(IEntity entity, IStateSnapshot[] initialStates)> entitiesWithState);
    bool MatchesController(IController controller);
    void ApplyController(IController controller, IEnumerable<ISystem> matchingSystems);
    void ApplyStateUpdates(IController controller, IEntity[] entities);
}

public interface ISystem<TMarkerInterface, TStateSnapshot> : ISystem
    where TMarkerInterface : IEntity
    where TStateSnapshot : struct
{
    void InitEntities(params (IEntity entity, TStateSnapshot? initialState)[] initialEntities);
    IReadOnlyList<IEntity> GetEntities();
    TStateSnapshot GetState(IEntity entity);
    void SetStates(IEnumerable<(IEntity entity, TStateSnapshot state)> stateUpdates);

    // Default interface methods to apply generic types
    void ISystem.InitEntities(IEnumerable<(IEntity entity, IStateSnapshot[] initialStates)> entitiesWithState)
    {
        // An entity is relevant to this system if it implements the marker interface or has an initial state for this system
        var matchingEntities = entitiesWithState
            .Select(e => (e.entity, initialState:
                e.initialStates.OfType<TStateSnapshot>().Select(s => (TStateSnapshot?)s).FirstOrDefault()))
            .Where(e => e.entity is TMarkerInterface || e.initialState != null)
            .ToArray();

        InitEntities(matchingEntities);
    }

    bool ISystem.MatchesController(IController controller)
    {
        return controller is IController<TStateSnapshot>;
    }

    void ISystem.ApplyController(IController controller, IEnumerable<ISystem> matchingSystems)
    {
        if (controller is IController<TStateSnapshot> typedController)
        {
            var entities = typedController.GetEntitiesToUpdate(GetEntities()).ToArray();
            foreach (var system in matchingSystems)
            {
                system.ApplyStateUpdates(controller, entities);
            }
        }
    }

    void ISystem.ApplyStateUpdates(IController controller, IEntity[] entities)
    {
        if (controller is IController<TStateSnapshot> typedController)
        {
            SetStates(entities.Select(entity =>
                (entity, typedController.GetNewState(entity, GetState(entity)))));
        }
    }
}
