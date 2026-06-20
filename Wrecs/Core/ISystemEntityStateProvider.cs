namespace Wrecs.Core;

public interface ISystemEntityStateProvider
{
    IStateSnapshot GetState(IEntity entity);
}
