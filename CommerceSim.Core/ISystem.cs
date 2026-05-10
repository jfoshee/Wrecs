namespace CommerceSim.Core;

public interface ISystem
{
    void PrepareUpdates();
    void ApplyUpdates();

    void InitEntities(IEnumerable<(IEntity entity, IStateSnapshot[] initialStates)> entitiesWithState);
}

public interface ISystem<TMarkerInterface, TStateSnapshot> : ISystem
    where TMarkerInterface : IEntity
    where TStateSnapshot : struct
{
    void InitEntities(params (IEntity entity, TStateSnapshot? initialState)[] initialEntities);
    IReadOnlyList<IEntity> GetEntities();
    TStateSnapshot GetState(IEntity entity);

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
}

public interface IEntityUpdate
{
    IEntity Entity { get; }
    // IStateSnapshot State { get; }
}

// public record EntityUpdate<TSystem, TStateSnapshot>(IEntity Entity, TStateSnapshot State) : IEntityUpdate
//     where TSystem : ISystem
//     where TStateSnapshot : IStateSnapshot<TSystem>;

public record EntityUpdate<TStateSnapshot>(IEntity Entity, TStateSnapshot State) : IEntityUpdate
    where TStateSnapshot : IStateSnapshot;

public record UpdateSet(IEnumerable<IEntityUpdate> Updates);

public interface IPrepareCompositeUpdates : ISystem
{
    IEnumerable<UpdateSet> PrepareCompositeUpdates();
}

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

public interface IControllableSystem : ISystem
{
    bool MatchesController(IController controller);
    void ApplyController(IController controller, IEnumerable<IControllableSystem> matchingSystems);
    void ApplyStateUpdates(IController controller, IEntity[] entities);
}

public interface IControllableSystem<TMarkerInterface, TStateSnapshot> : IControllableSystem, ISystem<TMarkerInterface, TStateSnapshot>
    where TMarkerInterface : IEntity
    where TStateSnapshot : struct
{
    void SetStates(IEnumerable<(IEntity entity, TStateSnapshot state)> stateUpdates);

    bool IControllableSystem.MatchesController(IController controller)
    {
        return controller is IController<TStateSnapshot>;
    }

    void IControllableSystem.ApplyController(IController controller, IEnumerable<IControllableSystem> matchingSystems)
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

    void IControllableSystem.ApplyStateUpdates(IController controller, IEntity[] entities)
    {
        if (controller is IController<TStateSnapshot> typedController)
        {
            SetStates(entities.Select(entity =>
                (entity, typedController.GetNewState(entity, GetState(entity)))));
        }
    }
}
