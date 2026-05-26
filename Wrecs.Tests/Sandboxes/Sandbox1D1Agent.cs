using Wrecs.Systems;

namespace Wrecs.Tests.Sandboxes;

// A sandbox where there is 1 primary agent who can move around
// in a 1D world and find resources and buy/sell resources.
// TODO: The agent can die if it does not get enough food.
public class Sandbox1D1Agent
{
    class MainAgent : ICommercialAgent, ISpatial1DAgent
    {
        public int Id { get; } = EntityId.Next();
        public string Name => nameof(MainAgent);

        IEnumerable<Type> IAgent.GetRequiredSnapshots() => [typeof(OfferListSnapshot)];

        Intent IAgent.GetIntent(IAgentContext context)
        {
            if (context.HasSnapshot<OfferListSnapshot>())
            {
                var offers = context.GetSnapshot<OfferListSnapshot>().Offers?.ToList() ?? [];
                var goodOffer = offers.OfType<BuyOffer>().FirstOrDefault(o => o.Price >= 10);
                if (goodOffer is not null)
                    return new(new TakeOfferDecision(goodOffer));
                return new(new DoNothingDecision());
            }
            return new Intent();
        }
    }

    class World
    {
        const int WorldSize = 10;
        private readonly Sim _sim = new();

        public World()
        {
            _sim.AddSystems(
                new Spatial1DSystem(),
                new WrapAroundSystem1D(size: WorldSize)
            );

            var mainAgent = new MainAgent();
            var goldSource = new ProximityResourceSource(resourcesGranted: 10, intervalTicks: 5, proximity: 1);
            _sim.InitEntities(
                (mainAgent, [new CommercialSnapshot(MoneyBalance: 100)]),
                (goldSource, [])
            );
        }

        public void Tick() => _sim.Tick();
    }

    [Fact(DisplayName = "Sandbox 1D, 1 Agent")]
    public void Run()
    {
        var world = new World();
        world.Tick();
    }
}
