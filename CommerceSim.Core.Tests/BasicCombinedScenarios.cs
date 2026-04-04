using CommerceSim.Core.Spatial;

namespace CommerceSim.Core.Tests;

class BasicComboAgent : ICommerceAgent, ISpatialAgent
{
    private readonly int _id = EntityId.Next();
    public int Id => _id;

    public string Name => nameof(BasicComboAgent);

    public int NextStep { get; set; } = 0;

    public Decision Decide(AgentStateSnapshot state, List<Offer> offers)
    {
        return new DoNothingDecision();
    }

    public int GetStep(int currentPosition)
    {
        var step = NextStep;
        NextStep = 0;
        return step;
    }
}

// Scenarios that combine spatial and commercial
public class BasicCombinedScenarios
{
    [Fact(DisplayName = "Agent receives from Proximity-aware Resource Source")]
    public void AgentReceivesFromProximityAwareResourceSource()
    {
        var agent = new BasicComboAgent();
        var source = new ProximityResourceSource(10, 1, 0);
        var sim = new Sim();
        sim.InitEntities(
            (agent, new(0, 0), 0),  // no money, sitting at origin
            (source, null, 5)  // sitting at position = +5
        );

        sim.Tick();

        // Nothing should have changed
        sim.GetAgentState(agent).Should().Be(new AgentStateSnapshot(0, 0));

        // Move the agent closer to the source
        agent.NextStep = 4;
        sim.Tick();

        // Agent should have new position, but still no resources
        sim.GetAgentState(agent).Should().Be(new AgentStateSnapshot(0, 0));
        sim.GetPosition(agent).Should().Be(4);

        // Move the agent on top of the source
        agent.NextStep = 1;
        sim.Tick();

        // Agent should have received resources from the source
        sim.GetPosition(agent).Should().Be(sim.GetPosition(source)); // at same position as source
        sim.GetAgentState(agent).Should().Be(new AgentStateSnapshot(0, 10));
    }
}
