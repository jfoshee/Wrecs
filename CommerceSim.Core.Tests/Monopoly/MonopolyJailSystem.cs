using System.Drawing;
using CommerceSim.Core.Spatial;

namespace CommerceSim.Core.Tests.Monopoly;

record struct MonopolyJailSnapshot(bool IsInJail, int TurnsRemaining) : IStateSnapshot<MonopolyJailSystem>;

class MonopolyJailSystem : ISystem<IMonopolyEntity, MonopolyJailSnapshot>
{
    private readonly List<IEntity> _entities = [];
    public IReadOnlyList<IEntity> GetEntities() => _entities;

    private readonly Dictionary<IEntity, int> _turnsRemaining = [];

    public void InitEntities(params (IEntity entity, MonopolyJailSnapshot? initialState)[] initialEntities)
    {
        _entities.Clear();
        foreach (var (entity, initialState) in initialEntities)
        {
            _entities.Add(entity);
            if (initialState.HasValue && initialState.Value.IsInJail)
                _turnsRemaining[entity] = initialState.Value.TurnsRemaining;
        }
    }

    public MonopolyJailSnapshot GetState(IEntity entity)
    {
        if (!_turnsRemaining.TryGetValue(entity, out int value))
            return new MonopolyJailSnapshot(false, 0);
        return new MonopolyJailSnapshot(true, value);
    }

    public void SetStates(IEnumerable<(IEntity entity, MonopolyJailSnapshot state)> stateUpdates)
    {
        foreach (var (entity, state) in stateUpdates)
        {
            if (state.IsInJail)
            {
                _turnsRemaining[entity] = state.TurnsRemaining;
            }
            else
            {
                _turnsRemaining.Remove(entity);
            }
        }
    }

    public void Tick()
    {
        foreach (var entity in _turnsRemaining.Keys.ToList())
        {
            _turnsRemaining[entity]--;
            if (_turnsRemaining[entity] <= 0)
                _turnsRemaining.Remove(entity);
        }
    }
}

class MonopolyJailController
    : IController<MonopolyJailSnapshot>, IRequire<MonopolyJailSystem>,
     IController<PositionSnapshot>, IRequire<SpatialSystem>

{
    private MonopolyJailSystem? _jailSystem;
    private SpatialSystem? _spatialSystem;

    public IEnumerable<IEntity> GetEntitiesToUpdate(IEnumerable<IEntity> allEntities)
    {
        // Return entities that are on the 30th tile which is the "Go to Jail" space
        return allEntities.Where(e => _spatialSystem!.GetState(e) == 30);
    }

    public MonopolyJailSnapshot GetNewState(IEntity entity, MonopolyJailSnapshot currentState)
    {
        // Snd them to jail for 3 turns
        return new MonopolyJailSnapshot(true, 3);
    }

    public PositionSnapshot GetNewState(IEntity entity, PositionSnapshot currentState)
    {
        // Move them to the Jail tile which is at position 10
        return new PositionSnapshot(10);
    }

    public void Inject(MonopolyJailSystem dependency) => _jailSystem = dependency;
    public void Inject(SpatialSystem dependency) => _spatialSystem = dependency;
}
