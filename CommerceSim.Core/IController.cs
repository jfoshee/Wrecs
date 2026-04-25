namespace CommerceSim.Core;

public interface IController;

public interface IController<TState> : IController
{
    IEnumerable<IEntity> GetEntitiesToUpdate(IEnumerable<IEntity> allEntities);
    TState GetNewState(IEntity entity, TState currentState);
}
