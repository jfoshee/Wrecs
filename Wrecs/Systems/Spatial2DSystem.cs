using System.Numerics;
using Wrecs.Core;

namespace Wrecs.Systems;

public readonly record struct Position2DSnapshot(Vector2 Position) : IStateSnapshot<Spatial2DSystem>
{
    public static implicit operator Vector2(Position2DSnapshot snapshot) => snapshot.Position;
    public static implicit operator Position2DSnapshot(Vector2 position) => new(position);
}

/// <summary>
/// Marker that an entity has a Spatial2D Position
/// </summary>
public interface ISpatial2DEntity : IEntity;

public record struct Move2DAction(Vector2 Step) : IIntentAction;

public interface ISpatial2DAgent : ISpatial2DEntity
{
    /// <summary>
    /// Given the agent's current position, returns the vector representing the step the agent wants to take.
    /// </summary>
    Intent GetIntent(Vector2 currentPosition);
}

public class Spatial2DSystem : ISystem<ISpatial2DEntity, Position2DSnapshot>, IAcceptUpdates<Position2DSnapshot>, ISpatialSystem
{
    private List<IEntity> _entities = [];
    private IEnumerable<ISpatial2DAgent> Agents => _entities.OfType<ISpatial2DAgent>();

    private readonly Dictionary<IEntity, Vector2> _entityPositions = [];
    private Dictionary<ISpatial2DAgent, Vector2> _pendingSteps = [];

    public Position2DSnapshot GetTypedState(IEntity entity) => new(_entityPositions[entity]);

    public IReadOnlyList<IEntity> GetEntities() => _entities;

    public void InitEntities(params (IEntity entity, Position2DSnapshot? initialState)[] initialEntities)
    {
        _entities = [.. initialEntities.Select(e => e.entity)];
        foreach (var (entity, initialState) in initialEntities)
        {
            _entityPositions[entity] = initialState ?? default;
        }
    }

    public void ApplyUpdates(IEnumerable<EntityUpdate<Position2DSnapshot>> updates)
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
            .Select(agent => (agent, intent: agent.GetIntent(_entityPositions[agent])))
            .Where(x => x.intent?.Actions?.OfType<Move2DAction>().Any() == true)
            .ToDictionary(x => x.agent, x => x.intent.Actions.OfType<Move2DAction>().First().Step);
    }

    public void ApplyInternalUpdates()
    {
        // Update Agents based on their steps
        foreach (var (agent, step) in _pendingSteps)
            _entityPositions[agent] += step;
    }

    public float GetDistance(IEntity e1, IEntity e2)
    {
        return Vector2.Distance(_entityPositions[e1], _entityPositions[e2]);
    }
}
