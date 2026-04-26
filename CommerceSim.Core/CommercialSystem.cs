namespace CommerceSim.Core;

public record struct Trade(Offer Offer,
                           CommercialSnapshot SellerState,
                           CommercialSnapshot BuyerState,
                           int Price,
                           int Resources,
                           string? ResourceType = null);

/// <summary>
/// Marker interface for entities in the commercial system (agents, sources, etc.)
/// </summary>
public interface ICommercialEntity : IEntity
{
}

public interface ICommercialController : IController<CommercialSnapshot>
{
}

public class CommercialSystem : ISystem<ICommercialEntity, CommercialSnapshot>
{
    private readonly List<IEntity> _entities = [];
    private IEnumerable<ICommercialAgent> Agents => _entities.OfType<ICommercialAgent>();
    private readonly List<ICommercialController> _controllers = [];
    private readonly Dictionary<IEntity, CommercialState> _states = [];
    private readonly List<Offer> _availableOffers = [];

    private readonly List<ITradePolicy> _tradePolicies = [
        new OfferSingleUsePolicy(),
        new CannotCreateResourcesPolicy(),
        new CannotCreateMoneyPolicy()
    ];

    public CommercialSnapshot GetState(IEntity entity) => new(_states[entity]);

    public IReadOnlyList<IEntity> GetEntities() => _entities;

    public IReadOnlyDictionary<int, CommercialSnapshot> GetStateSnapshot() =>
        _states.ToDictionary(kvp => kvp.Key.Id, kvp => new CommercialSnapshot(kvp.Value));

    public IReadOnlyDictionary<int, string> GetAgentNames() =>
        Agents.ToDictionary(a => a.Id, a => a.Name);

    public void InitEntities(params (IEntity entity, CommercialSnapshot? initialState)[] initialEntities)
    {
        _entities.Clear();
        _states.Clear();
        foreach (var (entity, initialState) in initialEntities)
        {
            _entities.Add(entity);
            _states[entity] = new(initialState ?? default);
        }
    }

    public void InitControllers(params ICommercialController[] controllers)
    {
        _controllers.Clear();
        _controllers.AddRange(controllers);
    }

    public void InitOffers(params Offer[] initialOffers)
    {
        _availableOffers.Clear();
        _availableOffers.AddRange(initialOffers);
    }

    // Advance simulation by one tick
    public void Tick()
    {
        // Decision making phase
        var decisions = new List<(ICommercialAgent Agent, Decision Decision)>();
        foreach (var agent in Agents)
        {
            var state = _states[agent];
            // Filter offers: include general offers + targeted offers for this agent
            var offersForAgent = _availableOffers
                .Where(o => o is not TargetedSellOffer targeted || targeted.Buyer == agent)
                .ToList();
            var decision = agent.Decide(new(state), offersForAgent);
            decisions.Add((agent, decision));
        }

        // Processing phase
        decisions = Shuffle(decisions);
        foreach (var (agent, decision) in decisions)
        {
            switch (decision)
            {
                case TakeOfferDecision takeOfferDecision:
                    var offer = takeOfferDecision.Offer;
                    _availableOffers.Remove(offer);
                    ProcessOffer(takeOfferDecision, _states[offer.Author], _states[agent]);
                    break;
                case MakeOfferDecision makeOfferDecision:
                    var newOffer = makeOfferDecision.Offer;
                    _availableOffers.Add(newOffer);
                    break;
            }
        }
        // Controller phase
        foreach (var controller in _controllers)
        {
            foreach (var entity in controller.GetEntitiesToUpdate(_entities))
            {
                var currentState = _states[entity];
                var newState = controller.GetNewState(entity, new(currentState));
                _states[entity] = new CommercialState(newState);
            }
        }

    }

    private static readonly Random _random = new();

    /// <summary>
    /// Randomly shuffle the order of decisions to ensure fairness in processing and avoid bias based on agent order.
    /// </summary>
    private static List<(ICommercialAgent Agent, Decision Decision)> Shuffle(List<(ICommercialAgent Agent, Decision Decision)> decisions)
    {
        return [.. decisions.OrderBy(_ => _random.Next())];
    }

    private void ProcessOffer(TakeOfferDecision decision,
                              CommercialState authorState,
                              CommercialState counterpartyState)
    {
        var offer = decision.Offer;
        // Construct a trade based on the offer
        var trade = new Trade(Offer: offer,
                              SellerState: offer is SellOffer ? new(authorState) : new(counterpartyState),
                              BuyerState: offer is BuyOffer ? new(authorState) : new(counterpartyState),
                              Price: offer.Price,
                              Resources: offer.Resources,
                              ResourceType: offer.ResourceType);
        // Check policies before executing the trade
        foreach (var policy in _tradePolicies)
        {
            if (!policy.CanExecute(trade))
                return;
        }
        // Execute the trade
        Execute(offer, authorState, counterpartyState);
        // Update policy state
        foreach (var policy in _tradePolicies)
        {
            policy.OnExecuted(trade);
        }
    }

    private static void Execute(Offer offer, CommercialState authorState, CommercialState counterpartyState)
    {
        CommercialState buyer, seller;
        switch (offer)
        {
            case BuyOffer buyOffer:
                buyer = authorState;
                seller = counterpartyState;
                break;
            case SellOffer sellOffer:
                buyer = counterpartyState;
                seller = authorState;
                break;
            default:
                throw new InvalidOperationException("Unknown offer type");
        }
        // Transfer money from buyer to seller
        buyer.MoneyBalance -= offer.Price;
        seller.MoneyBalance += offer.Price;
        // Transfer resources from seller to buyer
        buyer.AddResources(offer.ResourceType, offer.Resources);
        seller.AddResources(offer.ResourceType, -offer.Resources);
    }

    internal class CommercialState
    {
        // Internally use empty string for unitless (null) since Dictionary doesn't allow null keys
        private const string UnitlessKey = "";

        public int MoneyBalance { get; set; }
        public Dictionary<string, int> Inventory { get; } = [];

        public int GetResourceBalance(string? resourceType) =>
            Inventory.TryGetValue(resourceType ?? UnitlessKey, out var balance) ? balance : 0;

        public void AddResources(string? resourceType, int amount)
        {
            var key = resourceType ?? UnitlessKey;
            if (!Inventory.TryGetValue(key, out var current))
                current = 0;
            Inventory[key] = current + amount;
        }

        public CommercialState(CommercialSnapshot snapshot)
        {
            MoneyBalance = snapshot.MoneyBalance;
            foreach (var (type, amount) in snapshot.Inventory)
                Inventory[type] = amount;
        }
    }
}
