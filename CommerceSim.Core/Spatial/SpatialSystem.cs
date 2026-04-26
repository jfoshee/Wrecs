namespace CommerceSim.Core.Spatial;

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

public interface ISpatialController : IController<Position>
{
}

public class SpatialSystem : ISystem<ISpatialEntity, PositionSnapshot>
{
    private List<IEntity> _entities = [];
    private IEnumerable<ISpatialAgent> Agents => _entities.OfType<ISpatialAgent>();
    private readonly List<ISpatialController> _controllers = [];

    private readonly Dictionary<IEntity, Position> _entityPositions = [];

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

    public void InitControllers(params ISpatialController[] controllers)
    {
        _controllers.Clear();
        _controllers.AddRange(controllers);
    }

    public void Tick()
    {
        // Get steps that all agents want to take
        var agentSteps = Agents.ToDictionary(agent => agent, agent => agent.GetStep(_entityPositions[agent]));

        // Update Agents based on their steps
        foreach (var (agent, step) in agentSteps)
        {
            _entityPositions[agent] += step;
        }

        // Apply controllers to modify entity positions
        foreach (var controller in _controllers)
        {
            foreach (var entity in controller.GetEntitiesToUpdate(_entities))
            {
                var currentPosition = _entityPositions[entity];
                var newPosition = controller.GetNewState(entity, currentPosition);
                _entityPositions[entity] = newPosition;
            }
        }
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
