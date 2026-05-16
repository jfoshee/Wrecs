namespace Wrecs.Core.Spatial;

using Position = int;
using Vector = int;

public record struct PositionSnapshot(Position Position) : IStateSnapshot<SpatialSystem>
{
    public static implicit operator int(PositionSnapshot snapshot) => snapshot.Position;
    public static implicit operator PositionSnapshot(int position) => new(position);
}

/// <summary>
/// Marker that an entity has a Spatial Position
/// </summary>
public interface ISpatialEntity : IEntity;

public interface ISpatialAgent : ISpatialEntity
{
    /// <summary>
    /// Given the agent's current position, returns the vector representing the step the agent wants to take.
    /// </summary>
    Vector GetStep(Position currentPosition);
}

public interface ISpatialController : IController<PositionSnapshot>
{
}

public class SpatialSystem : IControllableSystem<ISpatialEntity, PositionSnapshot>
{
    private List<IEntity> _entities = [];
    private IEnumerable<ISpatialAgent> Agents => _entities.OfType<ISpatialAgent>();

    private readonly Dictionary<IEntity, Position> _entityPositions = [];
    private Dictionary<ISpatialAgent, Vector> _pendingSteps = [];

    public PositionSnapshot GetState(IEntity entity) => new(_entityPositions[entity]);

    public IReadOnlyList<IEntity> GetEntities() => _entities;

    public void InitEntities(params (IEntity entity, PositionSnapshot? initialState)[] initialEntities)
    {
        _entities = [.. initialEntities.Select(e => e.entity)];
        foreach (var (entity, initialState) in initialEntities)
        {
            _entityPositions[entity] = initialState ?? default;
        }
    }

    public void ApplyUpdates(IEnumerable<EntityUpdate<PositionSnapshot>> updates)
    {
        foreach (var update in updates)
        {
            _entityPositions[update.Entity] = update.State;
        }
    }

    public void PrepareInternalUpdates()
    {
        // Get steps that all agents want to take
        _pendingSteps = Agents.ToDictionary(agent => agent, agent => agent.GetStep(_entityPositions[agent]));
    }

    public void ApplyInternalUpdates()
    {
        // Update Agents based on their steps
        foreach (var (agent, step) in _pendingSteps)
            _entityPositions[agent] += step;
    }

    public double GetDistance(Position p1, Position p2)
    {
        return Math.Abs(p1 - p2);
    }

    public double GetDistance(IEntity e1, IEntity e2)
    {
        return GetDistance(_entityPositions[e1], _entityPositions[e2]);
    }
}
