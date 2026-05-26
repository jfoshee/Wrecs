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

        public Intent GetIntent(CommercialSnapshot state, List<Offer> offers)
        {
            var goodOffer = offers.OfType<BuyOffer>().FirstOrDefault(o => o.Price >= 10);
            if (goodOffer is not null)
                return new(new TakeOfferDecision(goodOffer));
            return new(new DoNothingDecision());
        }

        public Intent GetIntent(int currentPosition)
        {
            return new(new Move1DAction(0));
        }

        IEnumerable<Type> IAgent.GetRequiredSnapshots() => [typeof(CommercialSnapshot), typeof(OfferListSnapshot), typeof(PositionSnapshot)];

        Intent IAgent.GetIntent(IAgentContext context)
        {
            var actions = new List<IIntentAction>();

            if (context.HasSnapshot<OfferListSnapshot>())
            {
                var offers = context.GetSnapshot<OfferListSnapshot>().Offers?.ToList() ?? [];
                var intent1 = GetIntent(context.GetSnapshot<CommercialSnapshot>(), offers);
                actions.AddRange(intent1.Actions);
            }
            if (context.HasSnapshot<PositionSnapshot>())
            {
                var intent2 = GetIntent(context.GetSnapshot<PositionSnapshot>().Position);
                actions.AddRange(intent2.Actions);
            }

            return new Intent(actions);
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
