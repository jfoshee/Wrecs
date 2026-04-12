namespace CommerceSim.Core;

/// <summary>
/// Marker interface for entities that participate in taking turns.
/// </summary>
public interface ITakeTurns : IEntity;

public record struct TurnSnapshot(bool IsMyTurn) : IStateSnapshot<TurnSystem>;

public class TurnSystem : ISystem<ITakeTurns, TurnSnapshot>
{
    private List<IEntity> _entities = [];
    private int _currentTurnIndex = 0;

    public void InitEntities(params (IEntity entity, TurnSnapshot? initialState)[] initialEntities)
    {
        _entities = [.. initialEntities.Select(e => e.entity)];
        _currentTurnIndex = Array.FindIndex(initialEntities, e => e.initialState?.IsMyTurn == true);
        if (_currentTurnIndex == -1)
        {
            // Default to first entity if none have IsMyTurn set
            _currentTurnIndex = 0;
        }
    }

    public TurnSnapshot GetState(IEntity entity)
    {
        var index = _entities.IndexOf(entity);
        return new TurnSnapshot(index == _currentTurnIndex);
    }

    public void Tick()
    {
        // Next turn
        _currentTurnIndex = (_currentTurnIndex + 1) % _entities.Count;
    }

    public IEntity GetCurrentPlayer() => _entities[_currentTurnIndex];
}
