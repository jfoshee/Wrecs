using Wrecs.Systems;

namespace Wrecs.Tests.Sandboxes;

// A sandbox where there is 1 primary agent who can move around
// in a 1D world and find resources and buy/sell resources.
// TODO: The agent can die if it does not get enough food.
public class Sandbox1D1Agent
{
    enum ExplorerPhase { Searching, Collecting, GoingToSell, Returning }

    /// <summary>
    /// An agent that explores a 1D world to find a resource source, collects resources,
    /// then travels to a known buyer location to sell, and repeats the cycle.
    /// </summary>
    class ExplorerAgent : ISpatial1DAgent, ICommercialAgent
    {
        private const int BuyerPosition = 10;

        public int Id { get; } = EntityId.Next();
        public string Name => nameof(ExplorerAgent);

        public int? NextStep { get; set; } // used for testing to override the agent's next step

        private ExplorerPhase _phase = ExplorerPhase.Searching;
        private int _sourcePosition = -1;
        private int _lastInventory = 0;
        private int _dwellTick = 0;

        public int SellCount { get; private set; }

        public AgentIntent GetIntent(IAgentContext context)
        {
            if (NextStep.HasValue)
                return new AgentIntent(new Move1DAction(NextStep.Value));

            var snapshot = context.GetCommercialSnapshot();
            var inventory = snapshot.ResourceBalance;
            var position = context.GetSnapshot<Spatial1DSnapshot>().Position;
            var offers = context.GetSnapshot<OfferListSnapshot>().Offers?.ToList() ?? [];

            int step = 0;
            IAgentIntentAction? tradeAction = null;

            switch (_phase)
            {
                case ExplorerPhase.Searching:
                    if (inventory > _lastInventory)
                    {
                        // Resources appeared — source is at current position
                        _sourcePosition = position;
                        _phase = ExplorerPhase.GoingToSell;
                        step = Math.Sign(BuyerPosition - position);
                    }
                    else if (_dwellTick < 2)
                    {
                        // Sit on the spot to allow the source to detect proximity
                        step = 0;
                        _dwellTick++;
                    }
                    else
                    {
                        // No resource here after 2 ticks, move right
                        step = 1;
                        _dwellTick = 0;
                    }
                    break;

                case ExplorerPhase.Collecting:
                    if (inventory >= 10)
                    {
                        _phase = ExplorerPhase.GoingToSell;
                        step = Math.Sign(BuyerPosition - position);
                    }
                    else
                    {
                        step = 0; // stay at source until we have enough
                    }
                    break;

                case ExplorerPhase.GoingToSell:
                    if (position == BuyerPosition)
                    {
                        if (inventory > 0)
                        {
                            // At buyer — take their buy offer if available
                            var goodOffer = offers.OfType<BuyOffer>().FirstOrDefault(o => o.Price >= 10);
                            if (goodOffer is not null)
                                tradeAction = new TakeOfferDecision(goodOffer);
                            step = 0;
                        }
                        else
                        {
                            // Inventory depleted — sale complete, head back to source
                            SellCount++;
                            _phase = ExplorerPhase.Returning;
                            step = Math.Sign(_sourcePosition - BuyerPosition);
                        }
                    }
                    else
                    {
                        step = Math.Sign(BuyerPosition - position);
                    }
                    break;

                case ExplorerPhase.Returning:
                    if (position == _sourcePosition)
                    {
                        _phase = ExplorerPhase.Collecting;
                        step = 0;
                    }
                    else
                    {
                        step = Math.Sign(_sourcePosition - position);
                    }
                    break;
            }

            _lastInventory = inventory;

            var actions = new List<IAgentIntentAction> { new Move1DAction(step) };
            if (tradeAction is not null)
                actions.Insert(0, tradeAction);
            return new AgentIntent(actions);
        }
    }

    /// <summary>
    /// A stationary commercial agent at a known location that continuously posts buy offers for resources.
    /// </summary>
    class ResourceBuyer : ISpatial1DEntity, ICommercialAgent
    {
        public int Id { get; } = EntityId.Next();
        public string Name => nameof(ResourceBuyer);

        public AgentIntent GetIntent(IAgentContext context)
        {
            var offers = context.GetSnapshot<OfferListSnapshot>().Offers?.ToList() ?? [];
            var hasActiveBuyOffer = offers.Any(o => o.Author == this && !o.Used);
            if (!hasActiveBuyOffer)
                return new AgentIntent(new MakeOfferDecision(new BuyOffer(this, Price: 15, Resources: 10)));
            return AgentIntent.Empty;
        }
    }

    class World
    {
        const int WorldSize = 20;
        private readonly Sim _sim = new();

        public readonly ExplorerAgent Agent = new();
        public readonly ResourceBuyer Buyer = new();
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
                (Buyer, [new Spatial1DSnapshot(10), new CommercialSnapshot(MoneyBalance: 1000)]),
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

    [Fact(DisplayName = "Agent explores, collects resources, sells at buyer, and repeats")]
    public void AgentCollectsAndSellsInCycles()
    {
        var world = new World();

        // Run until first sale is expected (~tick 23) and verify intermediate state
        for (int i = 0; i < 25; i++)
            world.Tick();

        // After ~25 ticks: agent should have found the source and completed its first sale
        world.Agent.SellCount.Should().BeGreaterThanOrEqualTo(1,
            because: "agent should have found the source and sold its first load by tick 25");
        world.GetCommercialState(world.Agent).MoneyBalance.Should().BeGreaterThanOrEqualTo(15,
            because: "agent earns 15 money per sale");

        // Run more ticks to allow a second full collect-and-sell cycle (~tick 37)
        for (int i = 0; i < 20; i++)
            world.Tick();

        // After ~45 ticks: agent should have completed at least 2 sell cycles
        world.Agent.SellCount.Should().BeGreaterThanOrEqualTo(2,
            because: "agent should complete multiple collect-and-sell cycles");
        world.GetCommercialState(world.Agent).MoneyBalance.Should().BeGreaterThanOrEqualTo(30,
            because: "agent earns 15 per sale, so 2 sales = 30 money");

        // The buyer should have fewer funds (having paid for the resources)
        world.GetCommercialState(world.Buyer).MoneyBalance.Should().BeLessThan(1000,
            because: "buyer spent money buying resources");
    }
}
