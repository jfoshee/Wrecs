namespace CommerceSim.Core.Spatial;

using Position = int;
using Vector = int;

/// <summary>
/// Marker that an entity has a Spatial Position
/// </summary>
public interface ISpatialEntity : IEntity
{ }

public interface ISpatialAgent : ISpatialEntity
{
    /// <summary>
    /// Given the agent's current position, returns the vector representing the step the agent wants to take.
    /// </summary>
    Vector GetStep(Position currentPosition);
}

public interface ISpatialController
{
    IEnumerable<IEntity> GetEntitiesToMove(IEnumerable<IEntity> allEntities);
    Position GetNewPosition(IEntity entity, Position currentPosition);
}

public class SpatialSystem : ISystem<ISpatialEntity, Position>
{
    private List<IEntity> _entities = [];
    private IEnumerable<ISpatialAgent> Agents => _entities.OfType<ISpatialAgent>();
    private readonly List<ISpatialController> _controllers = [];

    private readonly Dictionary<IEntity, Position> _entityPositions = [];

    public Position GetState(IEntity entity) => _entityPositions[entity];

    public void InitEntities(params (IEntity entity, Position? position)[] initialEntities)
    {
        _entities = [.. initialEntities.Select(e => e.entity)];
        foreach (var (entity, initialPosition) in initialEntities)
        {
            _entityPositions[entity] = initialPosition ?? default;
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
            foreach (var entity in controller.GetEntitiesToMove(_entities))
            {
                var currentPosition = _entityPositions[entity];
                var newPosition = controller.GetNewPosition(entity, currentPosition);
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
