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

public class CommercialSystem : ISystem<ICommercialEntity, CommercialSnapshot>
{
    private List<IEntity> _entities = [];
    private IEnumerable<ICommercialAgent> agents => _entities.OfType<ICommercialAgent>();
    private readonly List<ISource> _sources = [];
    private readonly List<ISink> _sinks = [];
    private readonly Dictionary<IEntity, CommercialState> _states = [];
    private readonly List<Offer> _availableOffers = [];

    private readonly List<ITradePolicy> _tradePolicies = [
        new OfferSingleUsePolicy(),
        new CannotCreateResourcesPolicy(),
        new CannotCreateMoneyPolicy()
    ];
    private readonly List<IGrantPolicy> _grantPolicies = [
        new NoNegativeGrantsPolicy()
    ];
    private readonly List<IChargePolicy> _chargePolicies = [
        new NoNegativeChargesPolicy(),
        new NoForcingNegativeBalanceChargePolicy()
    ];

    public CommercialSnapshot GetState(IEntity entity) => new(_states[entity]);

    public IReadOnlyDictionary<int, CommercialSnapshot> GetStateSnapshot() =>
        _states.ToDictionary(kvp => kvp.Key.Id, kvp => new CommercialSnapshot(kvp.Value));

    public IReadOnlyDictionary<int, string> GetAgentNames() =>
        agents.ToDictionary(a => a.Id, a => a.Name);

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

    public void InitSources(params ISource[] sources)
    {
        _sources.Clear();
        _sources.AddRange(sources);
    }

    public void InitSinks(params ISink[] sinks)
    {
        _sinks.Clear();
        _sinks.AddRange(sinks);
    }

    public void InitOffers(params Offer[] initialOffers)
    {
        _availableOffers.Clear();
        _availableOffers.AddRange(initialOffers);
    }

    // Advance simulation by one tick
    public void Tick()
    {
        // Hack context
        var context = new Context(_states.Keys);

        // Grant phase
        // (Run first so that on first tick grants can be used for seeding agents)
        var grants = _sources.SelectMany(s => s.CreateGrants(context));
        foreach (var grant in grants)
        {
            // Skip grants that violate policies
            if (_grantPolicies.Any(p => !p.CanExecute(grant)))
                continue;
            var state = _states[grant.Recipient];
            state.MoneyBalance += grant.Money;
            state.AddResources(grant.ResourceType, grant.Resources);
        }

        // Decision making phase
        var decisions = new List<(ICommercialAgent Agent, Decision Decision)>();
        foreach (var agent in agents)
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

        // Charge phase
        // Process any sinks last so any chance to accumulate money/resources before charges
        var charges = _sinks.SelectMany(s => s.CreateCharges(context));
        foreach (var charge in charges)
        {
            // Skip charges that violate policies
            var state = _states[charge.Payor];
            if (_chargePolicies.Any(p => !p.CanExecute(charge, new(state))))
                continue;
            state.MoneyBalance -= charge.Money;
            state.AddResources(charge.ResourceType, -charge.Resources);
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

        /// <summary>
        /// Gets the balance for unitless resources (backward compatibility).
        /// </summary>
        public int ResourceBalance => GetResourceBalance(null);

        public int GetResourceBalance(string? resourceType) =>
            Inventory.TryGetValue(resourceType ?? UnitlessKey, out var balance) ? balance : 0;

        public void AddResources(string? resourceType, int amount)
        {
            var key = resourceType ?? UnitlessKey;
            if (!Inventory.TryGetValue(key, out var current))
                current = 0;
            Inventory[key] = current + amount;
        }

        public CommercialState(int moneyBalance = 0, int resourceBalance = 0)
        {
            MoneyBalance = moneyBalance;
            if (resourceBalance != 0)
                Inventory[UnitlessKey] = resourceBalance;
        }

        public CommercialState(CommercialSnapshot snapshot)
        {
            MoneyBalance = snapshot.MoneyBalance;
            foreach (var (type, amount) in snapshot.Inventory)
                Inventory[type] = amount;
        }
    }
}
