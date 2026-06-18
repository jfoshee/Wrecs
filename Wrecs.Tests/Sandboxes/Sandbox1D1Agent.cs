using Wrecs.Systems;

namespace Wrecs.Tests.Sandboxes;

class BasicComboAgent : ISpatial1DAgent, ICommercialAgent
{
    public int Id { get; } = EntityId.Next();
    public string Name => nameof(BasicComboAgent);

    public int NextStep { get; set; }

    public Intent GetIntent(IAgentContext context)
    {
        var actions = new List<IIntentAction>();
        var offers = context.GetSnapshot<OfferListSnapshot>().Offers?.ToList() ?? [];
        var goodOffer = offers.OfType<BuyOffer>().FirstOrDefault(o => o.Price >= 10);
        if (goodOffer is not null)
            actions.Add(new TakeOfferDecision(goodOffer));

        actions.Add(new Move1DAction(NextStep));

        return new Intent(actions);
    }
}

// A sandbox where there is 1 primary agent who can move around
// in a 1D world and find resources and buy/sell resources.
// TODO: The agent can die if it does not get enough food.
public class Sandbox1D1Agent
{
    class World
    {
        const int WorldSize = 10;
        private readonly Sim _sim = new();

        public readonly BasicComboAgent Agent = new();
        public readonly ProximityResourceSource Source = new(resourcesGranted: 10, intervalTicks: 1, proximity: 0);

        public World()
        {
            _sim.AddSystems(
                new MoneySystem(),
                new InventorySystem(),
                new OfferSystem(),
                new Spatial1DSystem(),
                new WrapAroundSystem1D(size: WorldSize),
                new ResourceSourcesController([Source])
            );
            _sim.InitEntities(
                (Agent, []),
                (Source, [new Spatial1DSnapshot(5)])
            );
        }

        public void Tick() => _sim.Tick();

        public CommercialSnapshot GetCommercialState(IEntity entity)
        {
            var moneyState = _sim.GetSystem<MoneySystem>().GetTypedState(entity);
            var inventoryState = _sim.GetSystem<InventorySystem>().GetTypedState(entity);
            return new CommercialSnapshot(moneyState, inventoryState);
        }

        public int GetPosition(IEntity entity) => _sim.GetPosition(entity);
    }

    [Fact(DisplayName = "Sandbox 1D, 1 Agent")]
    public void Run()
    {
        var world = new World();
        world.Agent.NextStep = 1;  // move right each tick
        var ticks = 10;
        for (int i = 0; i < ticks; i++)
            world.Tick();
    }

    [Fact(DisplayName = "Agent receives from Proximity-aware Resource Source")]
    public void AgentReceivesFromProximityAwareResourceSource()
    {
        var world = new World();

        world.Tick();

        // Nothing should have changed
        world.GetCommercialState(world.Agent).Should().Be(new CommercialSnapshot(0, 0));

        // Move the agent closer to the source
        world.Agent.NextStep = 4;
        world.Tick();

        // Agent should have new position, but still no resources
        world.GetCommercialState(world.Agent).Should().Be(new CommercialSnapshot(0, 0));
        world.GetPosition(world.Agent).Should().Be(4);

        // Move the agent on top of the source
        world.Agent.NextStep = 1;
        world.Tick();
        world.GetPosition(world.Agent).Should().Be(world.GetPosition(world.Source)); // at same position as source

        // Move agent 1 unit beyond the source
        world.Agent.NextStep = 1;
        world.Tick();
        // Agent should have received resources from the source
        world.GetCommercialState(world.Agent).Should().Be(new CommercialSnapshot(0, 10));
        world.GetPosition(world.Agent).Should().Be(6);

        // Agent has moved and no longer receives grant
        world.Tick();
        world.GetCommercialState(world.Agent).Should().Be(new CommercialSnapshot(0, 10));
    }
}
