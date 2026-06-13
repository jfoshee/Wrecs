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

public class Spatial1DSystem : ISystem<ISpatial1DEntity, Position1DSnapshot>, IAcceptUpdates<Position1DSnapshot>, ISpatialSystem
{
    private List<IEntity> _entities = [];
    private IEnumerable<ISpatial1DAgent> Agents => _entities.OfType<ISpatial1DAgent>();

    private readonly Dictionary<IEntity, Position> _entityPositions = [];
    private Dictionary<ISpatial1DAgent, Vector> _pendingSteps = [];

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

    public void PrepareInternalUpdates()
    {
        // Get steps that all agents want to take
        _pendingSteps = Agents
            .Select(agent =>
            {
                var ctx = new AgentContext();
                ctx.AddSnapshot<Position1DSnapshot>(_entityPositions[agent]);
                return (agent, intent: agent.GetIntent(ctx));
            })
            .Where(x => x.intent?.Actions?.OfType<Move1DAction>().Any() == true)
            .ToDictionary(x => x.agent, x => x.intent!.Actions.OfType<Move1DAction>().First().Step);
    }

    public void ApplyInternalUpdates()
    {
        // Update Agents based on their steps
        foreach (var (agent, step) in _pendingSteps)
            _entityPositions[agent] += step;
    }

    public float GetDistance(IEntity e1, IEntity e2)
    {
        return GetDistance(_entityPositions[e1], _entityPositions[e2]);
    }

    private static float GetDistance(Position p1, Position p2)
    {
        return Math.Abs(p1 - p2);
    }
}
