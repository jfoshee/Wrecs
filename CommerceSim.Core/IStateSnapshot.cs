namespace CommerceSim.Core;

public interface IStateSnapshot;

public interface IStateSnapshot<TSystem> : IStateSnapshot
    where TSystem : ISystem
{
}
