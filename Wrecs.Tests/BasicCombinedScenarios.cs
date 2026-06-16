using Wrecs.Systems;

namespace Wrecs.Tests;

class BasicComboAgent : ICommercialAgent, ISpatial1DAgent
{
    public int Id { get; } = EntityId.Next();

    public string Name => nameof(BasicComboAgent);

    public int NextStep { get; set; } = 0;

    public Intent GetIntent(CommercialSnapshot state, List<Offer> offers)
    {
        return new(new DoNothingDecision());
    }

    public Intent GetIntent(int currentPosition)
    {
        var step = NextStep;
        NextStep = 0;
        return new(new Move1DAction(step));
    }
    IEnumerable<Type> IAgent.GetRequiredSnapshots() => [typeof(CommercialSnapshot), typeof(OfferListSnapshot), typeof(Spatial1DSnapshot)];

    Intent IAgent.GetIntent(IAgentContext context)
    {
        var actions = new List<IIntentAction>();

        if (context.HasSnapshot<OfferListSnapshot>())
        {
            var offers = context.GetSnapshot<OfferListSnapshot>().Offers?.ToList() ?? [];
            var intent1 = GetIntent(context.GetSnapshot<CommercialSnapshot>(), offers);
            actions.AddRange(intent1.Actions);
        }

        if (context.HasSnapshot<Spatial1DSnapshot>())
        {
            var intent2 = GetIntent(context.GetSnapshot<Spatial1DSnapshot>().Position);
            actions.AddRange(intent2.Actions);
        }

        return new Intent(actions);
    }
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
            (agent, [new CommercialSnapshot(0, 0), new Spatial1DSnapshot(0)]),  // no money, sitting at origin
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
