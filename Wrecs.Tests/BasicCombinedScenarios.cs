using Wrecs.Systems;

namespace Wrecs.Tests;

class BasicComboAgent : ISpatial1DAgent, ICommercialAgent
{
    public int Id { get; } = EntityId.Next();
    public string Name => nameof(BasicComboAgent);

    public int NextStep { get; set; }

    public Intent GetIntent(IAgentContext context) => new([new Move1DAction(NextStep)]);
}

// Scenarios that combine spatial1d and commercial
public class BasicCombinedScenarios
{
    [Fact(DisplayName = "Agent receives from Proximity-aware Resource Source")]
    public void AgentReceivesFromProximityAwareResourceSource()
    {
        var agent = new BasicComboAgent();
        var source = new ProximityResourceSource(10, 1, 0);
        var sim = new CommercialSim();
        sim.AddSystem(new Spatial1DSystem());
        sim.AddSystem(new ResourceSourcesController([source]));
        sim.InitEntities(
            (agent, []),  // no money, sitting at origin
            (source, [new Spatial1DSnapshot(5)])  // sitting at position = +5
        );

        sim.Tick();

        // Nothing should have changed
        sim.GetCommercialState(agent).Should().Be(new CommercialSnapshot(0, 0));

        // Move the agent closer to the source
        agent.NextStep = 4;
        sim.Tick();

        // Agent should have new position, but still no resources
        sim.GetCommercialState(agent).Should().Be(new CommercialSnapshot(0, 0));
        sim.GetPosition(agent).Should().Be(4);

        // Move the agent on top of the source
        agent.NextStep = 1;
        sim.Tick();
        sim.GetPosition(agent).Should().Be(sim.GetPosition(source)); // at same position as source

        // Move agent 1 unit beyond the source
        agent.NextStep = 1;
        sim.Tick();
        // Agent should have received resources from the source
        sim.GetCommercialState(agent).Should().Be(new CommercialSnapshot(0, 10));
        sim.GetPosition(agent).Should().Be(6);

        // Agent has moved and no longer receives grant
        sim.Tick();
        sim.GetCommercialState(agent).Should().Be(new CommercialSnapshot(0, 10));
    }

    // TODO: Source has proximity > 0, Agent moves nearby within proximity
    // TODO: Source requires more than 1 tick to grant
}
