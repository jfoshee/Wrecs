using CommerceSim.Core.Spatial;

namespace CommerceSim.Core.Tests;

class BasicComboAgent : ICommercialAgent, ISpatialAgent
{
    public int Id { get; } = EntityId.Next();

    public string Name => nameof(BasicComboAgent);

    public int NextStep { get; set; } = 0;

    public Decision Decide(CommercialSnapshot state, List<Offer> offers)
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
        sim.InitControllers(new ResourceSourcesController([source]));
        sim.InitEntities(
            (agent, [new CommercialSnapshot(0, 0), new PositionSnapshot(0)]),  // no money, sitting at origin
            (source, [new PositionSnapshot(5)])  // sitting at position = +5
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

        // Agent should have received resources from the source
        sim.GetPosition(agent).Should().Be(sim.GetPosition(source)); // at same position as source
        sim.GetCommercialState(agent).Should().Be(new CommercialSnapshot(0, 10));

        // Move agent 1 unit beyond the source
        agent.NextStep = 1;
        sim.Tick();

        // Agent has moved and no longer receives grant
        sim.GetPosition(agent).Should().Be(6);
        sim.GetCommercialState(agent).Should().Be(new CommercialSnapshot(0, 10));
    }

    // TODO: Source has proximity > 0, Agent moves nearby within proximity
    // TODO: Source requires more than 1 tick to grant
}
