using Wrecs.Core;

namespace Wrecs.Systems;

public struct EndGameEvent : IEvent;

/// <summary>
/// Marker interface for entities that participate in taking turns.
/// </summary>
public interface ITakeTurns : IEntity;

/// <summary>
/// Represents turn ownership for the current engine tick.
/// A single player turn may span multiple engine ticks when <see cref="TurnSystem.PhasesPerTurn"/>
/// is greater than 1, which allows multi-step turn flows without rotating the active player every tick.
/// </summary>
public record struct TurnSnapshot(bool IsMyTurn, int Phase = 0) : IStateSnapshot<TurnSystem>;

public class TurnSystem :
    ISystem<ITakeTurns, TurnSnapshot>,
    ISystemEventHandler<EndGameEvent>,
    ISystemInternalUpdateApplier
{
    private bool _enabled = true;
    private List<IEntity> _entities = [];
    private int _currentTurnIndex = 0;
    private int _currentPhase;

    /// <summary>
    /// Number of engine ticks that belong to a single player's turn.
    /// This keeps turn ownership stable across multiple ticks when a domain needs a turn to span
    /// multiple steps, such as movement followed by resolution.
    /// </summary>
    public int PhasesPerTurn { get; }
    public int CurrentPhase => _currentPhase;
    public IEntity CurrentPlayer => _entities[_currentTurnIndex];

    public TurnSystem(int phasesPerTurn = 1)
    {
        if (phasesPerTurn <= 0)
            throw new ArgumentOutOfRangeException(nameof(phasesPerTurn), "must be greater than zero");

        PhasesPerTurn = phasesPerTurn;
    }

    public void InitEntities(params (IEntity entity, TurnSnapshot? initialState)[] initialEntities)
    {
        if (initialEntities.Length == 0)
            throw new ArgumentException("TurnSystem requires at least one entity to function", nameof(initialEntities));
        _entities = [.. initialEntities.Select(e => e.entity)];
        _currentTurnIndex = Array.FindIndex(initialEntities, e => e.initialState?.IsMyTurn == true);
        if (_currentTurnIndex == -1)
        {
            // Default to first entity if none have IsMyTurn set
            _currentTurnIndex = 0;
        }

        _currentPhase = initialEntities[_currentTurnIndex].initialState?.Phase ?? 0;
    }

    public IReadOnlyList<IEntity> GetEntities() => _entities;

    public TurnSnapshot GetTypedState(IEntity entity)
    {
        var index = _entities.IndexOf(entity);
        return new TurnSnapshot(index == _currentTurnIndex, _currentPhase);
    }

    public void ApplyInternalUpdates()
    {
        if (!_enabled)
            return;

        _currentPhase++;
        if (_currentPhase < PhasesPerTurn)
            return;

        _currentPhase = 0;
        _currentTurnIndex = (_currentTurnIndex + 1) % _entities.Count;
    }

    void ISystemEventHandler<EndGameEvent>.HandleTyped(EndGameEvent e)
    {
        _enabled = false;
        // Reset state
        _currentTurnIndex = 0;
        _currentPhase = 0;
    }
}
