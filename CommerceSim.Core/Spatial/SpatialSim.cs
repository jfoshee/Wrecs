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

public class SpatialSim : ISimulator
{
    private readonly List<ISpatialAgent> _agents = [];

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

    public void Tick()
    {
        // Get steps that all agents want to take
        var agentSteps = _agents.ToDictionary(agent => agent, agent => agent.GetStep(_entityPositions[agent]));

        // Update positions based on steps
        foreach (var (agent, step) in agentSteps)
        {
            _entityPositions[agent] += step;
        }
    }
}
