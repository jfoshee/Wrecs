using Wrecs.Core;

namespace Wrecs.Systems;

using Position = int;
using Vector = int;

public record struct Spatial1DSnapshot(Position Position) : IStateSnapshot<Spatial1DSystem>
{
    public static implicit operator int(Spatial1DSnapshot snapshot) => snapshot.Position;
    public static implicit operator Spatial1DSnapshot(int position) => new(position);
}

/// <summary>
/// Marker that an entity has a Spatial1D Position
/// </summary>
public interface ISpatial1DEntity : IEntity;

public record struct Move1DAction(Vector Step) : IIntentAction;

public interface ISpatial1DAgent : ISpatial1DEntity, IAgent
{
}

public class Spatial1DSystem :
    ISystem<ISpatial1DEntity, Spatial1DSnapshot>,
    IBuildAgentContext<Spatial1DSnapshot>,
    ITranslateIntent<Move1DAction>,
    IAcceptUpdates<Spatial1DSnapshot>,
    ISpatialSystem
{
    private List<IEntity> _entities = [];

    private readonly Dictionary<IEntity, Position> _entityPositions = [];

    public Spatial1DSnapshot GetTypedState(IEntity entity) => new(_entityPositions[entity]);

    public IReadOnlyList<IEntity> GetEntities() => _entities;

    public void InitEntities(params (IEntity entity, Spatial1DSnapshot? initialState)[] initialEntities)
    {
        _entities = [.. initialEntities.Select(e => e.entity)];
        foreach (var (entity, initialState) in initialEntities)
        {
            _entityPositions[entity] = initialState ?? default;
        }
    }

    public void ApplyUpdates(IEnumerable<EntityUpdate<Spatial1DSnapshot>> updates)
    {
        foreach (var update in updates)
        {
            _entityPositions[update.Entity] = update.State;
        }
    }

    public Spatial1DSnapshot? BuildSnapshot(IAgent agent) =>
        _entities.Contains(agent) ? _entityPositions[agent] : null;

    public UpdateSet TranslateIntent(IAgent agent, Move1DAction action)
    {
        if (_entities.Contains(agent))
        {
            var currentPosition = _entityPositions[agent];
            var newPosition = currentPosition + action.Step;
            return new([new EntityUpdate<Spatial1DSnapshot>(agent, new Spatial1DSnapshot(newPosition))]);
        }
        return new([]);
    }

    public void PrepareInternalUpdates() { }

    public void ApplyInternalUpdates() { }

    public float GetDistance(IEntity e1, IEntity e2)
    {
        return GetDistance(_entityPositions[e1], _entityPositions[e2]);
    }

    private static float GetDistance(Position p1, Position p2)
    {
        return Math.Abs(p1 - p2);
    }
}
