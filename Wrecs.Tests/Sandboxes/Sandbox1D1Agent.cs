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

        public Decision Decide(CommercialSnapshot state, List<Offer> offers)
        {
            return new DoNothingDecision();
        }

        public int GetStep(int currentPosition)
        {
            return 0;
        }
    }

    class World
    {
        private readonly Sim _sim = new();

        public World()
        {
            _sim.AddSystem(new Spatial1DSystem());

            var mainAgent = new MainAgent();
            _sim.InitEntities(
                (mainAgent, [new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 0)])
            );
            // _sim.AddSystem(new ProximityResourceSource(resourcesGranted: 10, intervalTicks: 2, proximity: 1));
        }
    }
}
