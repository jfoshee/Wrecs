namespace CommerceSim.Core;

public interface ISystem
{
    void Tick();
}

public interface ISystem<TMarkerInterface, TStateSnapshot> : ISystem
    where TMarkerInterface : IEntity
    where TStateSnapshot : struct
{
    TStateSnapshot GetState(IEntity entity);
    // IReadOnlyDictionary<int, TStateSnapshot> GetStateSnapshot();
}

public interface IStateSnapshot;
public interface IStateSnapshot<TSystem> : IStateSnapshot
    where TSystem : ISystem
{
}