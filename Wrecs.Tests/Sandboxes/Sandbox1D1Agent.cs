using Wrecs.Systems;

namespace Wrecs.Tests.Sandboxes;

// A sandbox where there is 1 primary agent who can move around
// in a 1D world and find resources and buy/sell resources.
// TODO: The agent can die if it does not get enough food.
public class Sandbox1D1Agent
{
    private const int MerchantBuyPrice = 15;

    public sealed record ExplorerObservation(int ResourceBalance,
                                             int MoneyBalance,
                                             bool CanCollect,
                                             bool CanSell,
                                             bool SourceVisible,
                                             bool BuyerVisible)
    {
        // Order of magnitude
        // This is a simple way to reduce the number of states in the Q-table.
        private static int OoM(int value) => value <= 0 ? 0 : (int)Math.Ceiling(Math.Log10(value));

        public static ExplorerObservation From(IAgentContext context)
        {
            var commercialState = context.GetCommercialSnapshot();
            var visibilityState = context.GetSnapshot<VisibilitySnapshot>();
            var offers = context.GetSnapshot<OfferListSnapshot>().Offers?.ToList() ?? [];

            var sourceVisible = visibilityState.VisibleEntities?.OfType<IResourceSource>().Any() ?? false;
            var buyerVisible = visibilityState.VisibleEntities?.OfType<ICommercialAgent>().Any() ?? false;
            var canSell = offers.OfType<BuyOffer>().Any(o => !o.Used);

            return new ExplorerObservation(
                ResourceBalance: OoM(commercialState.ResourceBalance),
                MoneyBalance: OoM(commercialState.MoneyBalance),
                CanCollect: sourceVisible,
                CanSell: canSell,
                SourceVisible: sourceVisible,
                BuyerVisible: buyerVisible
            );
        }
    }

    public enum ExplorerAction
    {
        Stay,
        MoveLeft,
        MoveRight,
        Sell
    }

    public interface IExplorerPolicy
    {
        ExplorerAction ChooseAction(ExplorerObservation observation);
    }

    public sealed class ExplorerAgent(IExplorerPolicy policy) :
        ISpatial1DAgent,
        ICommercialAgent,
        IAgentRequireSnapshot<VisibilitySnapshot>
    {
        public int Id { get; } = EntityId.Next();
        public string Name => nameof(ExplorerAgent);

        public AgentIntent GetIntent(IAgentContext context)
        {
            var observation = ExplorerObservation.From(context);
            var action = policy.ChooseAction(observation);

            return ToIntent(action, context);
        }

        private static AgentIntent ToIntent(ExplorerAction action, IAgentContext context)
        {
            return action switch
            {
                ExplorerAction.Stay => AgentIntent.Empty,
                ExplorerAction.MoveLeft => new AgentIntent(new Move1DAction(-1)),
                ExplorerAction.MoveRight => new AgentIntent(new Move1DAction(+1)),
                ExplorerAction.Sell => GetBestBuyOffer(context) is BuyOffer offer
                    ? new AgentIntent(new TakeOfferDecision(offer))
                    : AgentIntent.Empty, // w/out an offer Selling is not a valid action
                _ => throw new InvalidOperationException($"Unknown action: {action}")
            };
        }

        private static BuyOffer? GetBestBuyOffer(IAgentContext context) =>
            context.GetSnapshot<OfferListSnapshot>().Offers?
                .OfType<BuyOffer>()
                .Where(o => !o.Used)
                .OrderByDescending(o => o.Price)
                .FirstOrDefault();
    }

    enum ExplorerPhase { Searching, Collecting, GoingToSell, Returning }

    public sealed class PrescriptedExplorerPolicy(ExplorerAction defaultAction = ExplorerAction.Stay) : IExplorerPolicy
    {
        private readonly Queue<ExplorerAction> _actions = [];

        public void Enqueue(params ExplorerAction[] actions)
        {
            foreach (var action in actions)
                _actions.Enqueue(action);
        }

        public ExplorerAction ChooseAction(ExplorerObservation observation) =>
            _actions.TryDequeue(out var action) ? action : defaultAction;
    }

    public sealed class ScriptedExplorerPolicy : IExplorerPolicy
    {
        private ExplorerPhase _phase = ExplorerPhase.Searching;
        private int _lastInventory = 0;

        public ExplorerAction ChooseAction(ExplorerObservation observation)
        {
            var action = _phase switch
            {
                ExplorerPhase.Searching => Search(observation),
                ExplorerPhase.Collecting => Collect(observation),
                ExplorerPhase.GoingToSell => GoSell(observation),
                ExplorerPhase.Returning => Return(observation),
                _ => ExplorerAction.Stay
            };

            _lastInventory = observation.ResourceBalance;
            return action;
        }

        private ExplorerAction Search(ExplorerObservation observation)
        {
            if (observation.ResourceBalance > _lastInventory)
            {
                _phase = ExplorerPhase.GoingToSell;
                return ExplorerAction.MoveRight;
            }

            return ExplorerAction.MoveRight;
        }

        private ExplorerAction Collect(ExplorerObservation observation)
        {
            if (observation.ResourceBalance > 0)
            {
                _phase = ExplorerPhase.GoingToSell;
                return ExplorerAction.MoveRight;
            }

            return observation.CanCollect
                ? ExplorerAction.Stay
                : ExplorerAction.MoveRight;
        }

        private ExplorerAction GoSell(ExplorerObservation observation)
        {
            if (observation.ResourceBalance <= 0)
            {
                _phase = ExplorerPhase.Returning;
                return ExplorerAction.MoveLeft;
            }

            return observation.CanSell
                ? ExplorerAction.Sell
                : ExplorerAction.MoveRight;
        }

        private ExplorerAction Return(ExplorerObservation observation)
        {
            if (observation.ResourceBalance > _lastInventory)
            {
                _phase = ExplorerPhase.Collecting;
                return ExplorerAction.Stay;
            }

            return ExplorerAction.MoveLeft;
        }
    }

    /// <summary>
    /// A stationary commercial agent at a known location that continuously posts buy offers for resources.
    /// </summary>
    internal class Merchant : ISpatial1DEntity, ICommercialAgent, IAgentRequireSnapshot<VisibilitySnapshot>
    {
        public int Id { get; } = EntityId.Next();
        public string Name => nameof(Merchant);

        public AgentIntent GetIntent(IAgentContext context)
        {
            var visibleEntities = context.GetSnapshot<VisibilitySnapshot>().VisibleEntities?.ToList() ?? [];
            var target = visibleEntities.OfType<ICommercialAgent>().FirstOrDefault();
            if (target is null)
                return AgentIntent.Empty;

            var offers = context.GetSnapshot<OfferListSnapshot>().Offers?.ToList() ?? [];
            var hasActiveOfferToTarget = offers.Any(o =>
                o is TargetedBuyOffer targeted && targeted.Author == this && targeted.Seller == target && !o.Used);
            if (!hasActiveOfferToTarget)
                return new AgentIntent(new MakeOfferDecision(new TargetedBuyOffer(this, target, Price: 15, Resources: 10)));
            return AgentIntent.Empty;
        }
    }

    internal class World
    {
        const int WorldSize = 20;
        private readonly Sim _sim = new();

        public readonly ExplorerAgent Agent;
        public readonly Merchant Buyer = new();
        public readonly ProximityResourceSource Source = new(resourcesGranted: 10, intervalTicks: 1, proximity: 0);
        public readonly IEntity LearningRampEntity = new Entity(nameof(LearningRampEntity));

        public World(IExplorerPolicy? explorerPolicy = null, RampSnapshot? learningRamp = null)
        {
            Agent = new ExplorerAgent(explorerPolicy ?? new ScriptedExplorerPolicy());

            _sim.AddSystems(
                new MoneySystem(),
                new InventorySystem(),
                new OfferSystem(),
                new Spatial1DSystem(),
                new WrapAroundSystem1D(size: WorldSize),
                new ResourceSourcesController([Source]),
                new VisibilitySystem(maxDistance: 1),
                new RampSystem(),
                new RampEventDelegateHandler(LearningRampEntity, e => LearningRampHandler(e))
            );
            _sim.InitEntities(
                (Agent, []),
                (Buyer, [new Spatial1DSnapshot(10), new CommercialSnapshot(MoneyBalance: 1000)]),
                (Source, [new Spatial1DSnapshot(5)]),
                (LearningRampEntity, [learningRamp ?? default(RampSnapshot)])
            );
        }

        public Action<RampEvent> LearningRampHandler { get; set; } = _ => { };

        public void Tick() => _sim.Tick();

        public CommercialSnapshot GetCommercialState(IEntity entity)
        {
            var moneyState = _sim.GetSystem<MoneySystem>().GetTypedState(entity);
            var inventoryState = _sim.GetSystem<InventorySystem>().GetTypedState(entity);
            return new CommercialSnapshot(moneyState, inventoryState);
        }

        public int GetPosition(IEntity entity) => _sim.GetPosition(entity);

        public int GetCompletedSaleCount(IEntity seller) =>
            GetCommercialState(seller).MoneyBalance / MerchantBuyPrice;
    }

    [Fact(DisplayName = "Sandbox 1D, 1 Agent")]
    public void Run()
    {
        var world = new World(new PrescriptedExplorerPolicy(ExplorerAction.MoveRight));
        var ticks = 10;
        for (int i = 0; i < ticks; i++)
            world.Tick();
    }

    [Fact(DisplayName = "Agent receives from Proximity-aware Resource Source")]
    public void AgentReceivesFromProximityAwareResourceSource()
    {
        var policy = new PrescriptedExplorerPolicy();
        var world = new World(policy);

        world.Tick();

        // Nothing should have changed
        world.GetCommercialState(world.Agent).Should().Be(new CommercialSnapshot(0, 0));

        // Move the agent closer to the source
        policy.Enqueue(
            ExplorerAction.MoveRight,
            ExplorerAction.MoveRight,
            ExplorerAction.MoveRight,
            ExplorerAction.MoveRight,
            ExplorerAction.MoveRight);
        for (int i = 0; i < 5; i++)
            world.Tick();

        // Agent should have moved onto the source position, but still no resources yet
        world.GetCommercialState(world.Agent).Should().Be(new CommercialSnapshot(0, 0));
        world.GetPosition(world.Agent).Should().Be(5);

        // Stay on the source long enough to receive the grant
        policy.Enqueue(ExplorerAction.Stay);
        world.Tick();
        world.GetPosition(world.Agent).Should().Be(world.GetPosition(world.Source)); // at same position as source
        world.GetCommercialState(world.Agent).ResourceBalance.Should().BeGreaterThanOrEqualTo(10);

        // Move agent 1 unit beyond the source
        policy.Enqueue(ExplorerAction.MoveRight);
        world.Tick();
        // Agent should have received resources from the source
        world.GetCommercialState(world.Agent).ResourceBalance.Should().BeGreaterThanOrEqualTo(10);
        world.GetPosition(world.Agent).Should().Be(6);

        // Agent has moved and no longer receives grant
        world.Tick();
        world.GetCommercialState(world.Agent).ResourceBalance.Should().BeGreaterThanOrEqualTo(10);
    }

    [Fact(DisplayName = "Agent explores, collects resources, sells at buyer, and repeats")]
    public void AgentCollectsAndSellsInCycles()
    {
        var world = new World();

        // Run until first sale is expected (~tick 23) and verify intermediate state
        for (int i = 0; i < 25; i++)
            world.Tick();

        // After ~25 ticks: agent should have found the source and completed its first sale
        world.GetCompletedSaleCount(world.Agent).Should().BeGreaterThanOrEqualTo(1,
            because: "agent should have found the source and sold its first load by tick 25");
        world.GetCommercialState(world.Agent).MoneyBalance.Should().BeGreaterThanOrEqualTo(15,
            because: "agent earns 15 money per sale");

        // Run more ticks to allow a second full collect-and-sell cycle (~tick 37)
        for (int i = 0; i < 20; i++)
            world.Tick();

        // After ~45 ticks: agent should have completed at least 2 sell cycles
        world.GetCompletedSaleCount(world.Agent).Should().BeGreaterThanOrEqualTo(2,
            because: "agent should complete multiple collect-and-sell cycles");
        world.GetCommercialState(world.Agent).MoneyBalance.Should().BeGreaterThanOrEqualTo(30,
            because: "agent earns 15 per sale, so 2 sales = 30 money");

        // The buyer should have fewer funds (having paid for the resources)
        world.GetCommercialState(world.Buyer).MoneyBalance.Should().BeLessThan(1000,
            because: "buyer spent money buying resources");
    }
}
