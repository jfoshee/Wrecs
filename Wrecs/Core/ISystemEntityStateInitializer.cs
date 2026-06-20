namespace Wrecs.Core;

public interface ISystemEntityStateInitializer : ISystem
{
    void InitEntities(IEnumerable<(IEntity entity, IStateSnapshot[] initialStates)> entitiesWithState);
    IReadOnlyList<IEntity> GetEntities();
}
