namespace CommerceSim.Core;

public interface IController<TState>
{
    IEnumerable<IEntity> GetEntitiesToUpdate(IEnumerable<IEntity> allEntities);
    TState GetNewState(IEntity entity, TState currentState);
}
