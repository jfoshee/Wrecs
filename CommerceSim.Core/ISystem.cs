namespace CommerceSim.Core;

public interface ISystem
{
    void Tick();

    // Helper methods to avoid reflection in Sim.cs
    void ApplyController(IController controller, IEnumerable<ISystem> matchingSystems);
    bool MatchesController(IController controller);
    void ApplyStateUpdates(IController controller, IEntity[] entities);
    void InitEntities(IEnumerable<(IEntity entity, IStateSnapshot[] initialStates)> entitiesWithState);
}

public interface ISystem<TMarkerInterface, TStateSnapshot> : ISystem
    where TMarkerInterface : IEntity
    where TStateSnapshot : struct
{
    void InitEntities(params (IEntity entity, TStateSnapshot? initialState)[] initialEntities);
    IReadOnlyList<IEntity> GetEntities();
    TStateSnapshot GetState(IEntity entity);
    void SetStates(IEnumerable<(IEntity entity, TStateSnapshot state)> stateUpdates);

    // Default implementations to avoid reflection in Sim.cs
    void ISystem.InitEntities(IEnumerable<(IEntity entity, IStateSnapshot[] initialStates)> entitiesWithState)
    {
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

public interface IStateSnapshot;
public interface IStateSnapshot<TSystem> : IStateSnapshot
    where TSystem : ISystem
{
}