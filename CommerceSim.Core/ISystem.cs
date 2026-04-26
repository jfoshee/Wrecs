namespace CommerceSim.Core;

public interface ISystem
{
    void Tick();
}

public interface ISystem<TMarkerInterface, TStateSnapshot> : ISystem
    where TMarkerInterface : IEntity
    where TStateSnapshot : struct
{
    void InitEntities(params (IEntity entity, TStateSnapshot? initialState)[] initialEntities);
    IReadOnlyList<IEntity> GetEntities();
    TStateSnapshot GetState(IEntity entity);
    void SetStates(IEnumerable<(IEntity entity, TStateSnapshot state)> stateUpdates);
    // IReadOnlyDictionary<int, TStateSnapshot> GetStateSnapshot();
}

public interface IStateSnapshot;
public interface IStateSnapshot<TSystem> : IStateSnapshot
    where TSystem : ISystem
{
}