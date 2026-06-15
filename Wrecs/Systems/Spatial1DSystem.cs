using Wrecs.Core;

namespace Wrecs.Systems;

using Position = int;
using Vector = int;

public record struct Position1DSnapshot(Position Position) : IStateSnapshot<Spatial1DSystem>
{
    public static implicit operator int(Position1DSnapshot snapshot) => snapshot.Position;
    public static implicit operator Position1DSnapshot(int position) => new(position);
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
    ISystem<ISpatial1DEntity, Position1DSnapshot>,
    IBuildAgentContext,
    ITranslateIntent<Move1DAction>,
    IAcceptUpdates<Position1DSnapshot>,
    ISpatialSystem
{
    private List<IEntity> _entities = [];
    private IEnumerable<ISpatial1DAgent> Agents => _entities.OfType<ISpatial1DAgent>();

    private readonly Dictionary<IEntity, Position> _entityPositions = [];

    public Position1DSnapshot GetTypedState(IEntity entity) => new(_entityPositions[entity]);

    public IReadOnlyList<IEntity> GetEntities() => _entities;

    public void InitEntities(params (IEntity entity, Position1DSnapshot? initialState)[] initialEntities)
    {
        _entities = [.. initialEntities.Select(e => e.entity)];
        foreach (var (entity, initialState) in initialEntities)
        {
            _entityPositions[entity] = initialState ?? default;
        }
    }

    public void ApplyUpdates(IEnumerable<EntityUpdate<Position1DSnapshot>> updates)
    {
        foreach (var update in updates)
        {
            _entityPositions[update.Entity] = update.State;
        }
    }

    public void PopulateContext(IAgent agent, AgentContext context)
    {
        if (_entities.Contains(agent))
        {
            var agentPosition = _entityPositions[agent];
            context.AddSnapshot(new Position1DSnapshot(agentPosition));
        }
    }

    public UpdateSet TranslateIntent(IAgent agent, Move1DAction action)
    {
        if (_entities.Contains(agent))
        {
            var currentPosition = _entityPositions[agent];
            var newPosition = currentPosition + action.Step;
            return new([new EntityUpdate<Position1DSnapshot>(agent, new Position1DSnapshot(newPosition))]);
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
