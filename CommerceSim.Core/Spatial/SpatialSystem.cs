namespace CommerceSim.Core.Spatial;

using Position = int;
using Vector = int;

public interface ISpatialAgent : IEntity
{
    /// <summary>
    /// Given the agent's current position, returns the vector representing the step the agent wants to take.
    /// </summary>
    Vector GetStep(Position currentPosition);
}

public interface ISpatialController
{
    IEnumerable<IEntity> GetEntitiesToMove();
    Position GetNewPosition(IEntity entity, Position currentPosition);
}

public class SpatialSystem : ISystem
{
    private readonly List<ISpatialAgent> _agents = [];
    private readonly List<ISpatialController> _controllers = [];

    private readonly Dictionary<IEntity, Position> _entityPositions = [];

    public Position GetPosition(IEntity entity) => _entityPositions[entity];

    public void InitAgents(params (ISpatialAgent agent, Position position)[] initialAgents)
    {
        _agents.Clear();
        _entityPositions.Clear();
        foreach (var (agent, position) in initialAgents)
        {
            _agents.Add(agent);
            _entityPositions[agent] = position;
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
        var agentSteps = _agents.ToDictionary(agent => agent, agent => agent.GetStep(_entityPositions[agent]));

        // Update Agents based on their steps
        foreach (var (agent, step) in agentSteps)
        {
            _entityPositions[agent] += step;
        }

        // Apply controllers to modify entity positions
        foreach (var controller in _controllers)
        {
            foreach (var entity in controller.GetEntitiesToMove())
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
