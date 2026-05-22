using Wrecs.Core;

namespace Wrecs.Systems.Commercial;

public record struct OfferSnapshot(ICommercialAgent? Seller, ICommercialAgent? Buyer, int Price, int Resources, string? ResourceType = null);

public record struct OfferListSnapshot(List<OfferSnapshot>? OfferSnapshots) : IStateSnapshot<OfferSystem>
{
    public override readonly string ToString()
    {
        // When this struct is defaulted OfferSnapshots is null
        var snapshots = OfferSnapshots ?? [];
        return "[" + string.Join(", ", snapshots.Select(x => x.ToString())) + "]";
    }
}

public record struct RemoveOfferOperation(Offer? Offer) : IStateSnapshot<OfferSystem>;

public class OfferSystem :
    ISystem<ICommercialAgent, OfferListSnapshot>,
    IPrepareSharedUpdates,
    IAcceptUpdates<RemoveOfferOperation>,
    IRequire<MoneySystem>,
    IRequire<InventorySystem>
{
    private readonly List<IEntity> _entities = [];
    private readonly Dictionary<IEntity, List<Offer>> _stateMap = [];
    private MoneySystem? _moneySystem;
    private InventorySystem? _inventorySystem;
    private List<(ICommercialAgent Agent, Decision Decision)> _pendingDecisions = [];

    private readonly List<ITradePolicy> _tradePolicies = [
        new OfferSingleUsePolicy(),
        new CannotCreateResourcesPolicy(),
        new CannotCreateMoneyPolicy()
    ];

    public void Inject(MoneySystem dependency) => _moneySystem = dependency;
    public void Inject(InventorySystem dependency) => _inventorySystem = dependency;

    public void InitEntities(params (IEntity entity, OfferListSnapshot? initialState)[] initialEntities)
    {
        _entities.Clear();
        _stateMap.Clear();
        foreach (var (entity, initialState) in initialEntities)
        {
            _entities.Add(entity);
            if (initialState.HasValue)
                _stateMap[entity] = State(initialState.Value);
        }
    }

    public IReadOnlyList<IEntity> GetEntities() => _entities;

    public OfferListSnapshot GetTypedState(IEntity entity)
    {
        var state = _stateMap.GetValueOrDefault(entity);
        return Snapshot(state);
    }

    public void PrepareInternalUpdates()
    {
        var allOffers = _stateMap.Values.SelectMany(x => x).ToList();

        // Decision making phase
        var decisions = new List<(ICommercialAgent Agent, Decision Decision)>();
        foreach (var agent in _entities.OfType<ICommercialAgent>())
        {
            // Filter offers: include general offers + targeted offers for this agent
            var offersForAgent = allOffers
                .Where(o => o is not TargetedSellOffer targeted || targeted.Buyer == agent)
                .ToList();
            var commercialState = BuildCommercialSnapshot(agent);
            var decision = agent.Decide(commercialState, offersForAgent);
            decisions.Add((agent, decision));
        }

        _pendingDecisions = Shuffle(decisions);
    }

    public IEnumerable<UpdateSet> PrepareSharedUpdates()
    {
        // Take-offer decisions need to be processed here because they affect multiple systems
        List<UpdateSet> updateSets = [];
        foreach (var (agent, decision) in _pendingDecisions)
        {
            if (decision is TakeOfferDecision takeOfferDecision)
            {
                var offer = takeOfferDecision.Offer;
                var updateSet = ProcessOffer(agent, offer);
                updateSets.Add(updateSet);
            }
        }
        return updateSets;
    }

    public void ApplyInternalUpdates()
    {
        // Make-offer decisions can be handled here because they only affect the offer system's own state
        foreach (var (agent, decision) in _pendingDecisions)
        {
            if (decision is MakeOfferDecision makeOfferDecision)
            {
                var newOffer = makeOfferDecision.Offer;
                AddOffer(agent, newOffer);
            }
        }
    }

    public void ApplyUpdates(IEnumerable<EntityUpdate<RemoveOfferOperation>> updates)
    {
        foreach (var update in updates)
        {
            var operation = update.State;
            if (operation.Offer is not null)
                RemoveOffer(operation.Offer.Author, operation.Offer);
        }
    }

    private void AddOffer(ICommercialAgent agent, Offer offer)
    {
        if (!_stateMap.ContainsKey(agent))
            _stateMap[agent] = [];
        _stateMap[agent].Add(offer);
    }

    private void RemoveOffer(ICommercialAgent author, Offer offer)
    {
        if (!_stateMap.TryGetValue(author, out var offers))
            return;
        offers.Remove(offer);
        if (offers.Count == 0)
            _stateMap.Remove(author);
    }

    private UpdateSet ProcessOffer(ICommercialAgent taker, Offer offer)
    {
        var author = offer.Author;
        var authorState = BuildCommercialSnapshot(author);
        var counterpartyState = BuildCommercialSnapshot(taker);

        // Construct a trade based on the offer
        var trade = new Trade(Offer: offer,
                              SellerState: offer is SellOffer ? authorState : counterpartyState,
                              BuyerState: offer is BuyOffer ? authorState : counterpartyState,
                              Price: offer.Price,
                              Resources: offer.Resources,
                              ResourceType: offer.ResourceType);
        // Check policies before executing the trade
        foreach (var policy in _tradePolicies)
        {
            if (!policy.CanExecute(trade))
                return new UpdateSet([]);
        }
        // Execute the trade
        var updates = Execute(offer, taker, authorState, counterpartyState);
        // Update policy state
        foreach (var policy in _tradePolicies)
        {
            policy.OnExecuted(trade);
        }
        return new UpdateSet(updates);
    }

    private CommercialSnapshot BuildCommercialSnapshot(IEntity entity) =>
        new(_moneySystem!.GetTypedState(entity), _inventorySystem!.GetTypedState(entity));

    private static IEnumerable<IEntityUpdate> Execute(Offer offer,
                                                      ICommercialAgent counterparty,
                                                      CommercialSnapshot authorState,
                                                      CommercialSnapshot counterpartyState)
    {
        ICommercialAgent buyer, seller;
        CommercialSnapshot buyerState, sellerState;
        switch (offer)
        {
            case BuyOffer:
                (buyer, buyerState) = (offer.Author, authorState);
                (seller, sellerState) = (counterparty, counterpartyState);
                break;
            case SellOffer:
                (buyer, buyerState) = (counterparty, counterpartyState);
                (seller, sellerState) = (offer.Author, authorState);
                break;
            default:
                throw new InvalidOperationException("Unknown offer type");
        }
        // Transfer money from buyer to seller
        var buyerMoneyBalance = buyerState.MoneyBalance - offer.Price;
        var sellerMoneyBalance = sellerState.MoneyBalance + offer.Price;
        // Transfer resources from seller to buyer
        var buyerResourceBalance = buyerState.GetResourceBalance(offer.ResourceType) + offer.Resources;
        var sellerResourceBalance = sellerState.GetResourceBalance(offer.ResourceType) - offer.Resources;
        return
        [
            new EntityUpdate<MoneySnapshot>(buyer, new MoneySnapshot(buyerMoneyBalance)),
            new EntityUpdate<MoneySnapshot>(seller, new MoneySnapshot(sellerMoneyBalance)),
            new EntityUpdate<InventorySnapshot>(buyer, UpdatedInventorySnapshot(buyerState.Inventory, buyerResourceBalance, offer.ResourceType)),
            new EntityUpdate<InventorySnapshot>(seller, UpdatedInventorySnapshot(sellerState.Inventory, sellerResourceBalance, offer.ResourceType)),
            new EntityUpdate<RemoveOfferOperation>(offer.Author, new RemoveOfferOperation(offer))
        ];
    }

    private static InventorySnapshot UpdatedInventorySnapshot(InventorySnapshot inventory, int newResourceBalance, string? resourceType)
    {
        var key = resourceType ?? "";
        var updated = inventory.Inventory
            .Where(i => i.Type != key)
            .Append((key, newResourceBalance));
        return new InventorySnapshot(updated);
    }

    private static List<Offer> State(OfferListSnapshot snapshot)
    {
        if (snapshot.OfferSnapshots is null)
            return [];
        return [.. snapshot.OfferSnapshots.Select(State)];
    }

    private static Offer State(OfferSnapshot snapshot)
    {
        if (snapshot.Buyer is not null && snapshot.Seller is not null)
            return new TargetedSellOffer(snapshot.Seller, snapshot.Buyer, snapshot.Price, snapshot.Resources, snapshot.ResourceType);
        if (snapshot.Buyer is not null)
            return new BuyOffer(snapshot.Buyer, snapshot.Price, snapshot.Resources, snapshot.ResourceType);
        if (snapshot.Seller is not null)
            return new SellOffer(snapshot.Seller, snapshot.Price, snapshot.Resources, snapshot.ResourceType);
        throw new ArgumentException("Buyer and Seller null", nameof(snapshot));
    }

    private static OfferListSnapshot Snapshot(List<Offer>? offers)
    {
        if (offers is null)
            return default;
        return new([.. offers.Select(Snapshot)]);
    }

    private static OfferSnapshot Snapshot(Offer offer)
    {
        if (offer is TargetedSellOffer targetedSellOffer)
            return new(targetedSellOffer.Seller, targetedSellOffer.Buyer, targetedSellOffer.Price, targetedSellOffer.Resources, targetedSellOffer.ResourceType);
        if (offer is BuyOffer buyOffer)
            return new(null, buyOffer.Buyer, buyOffer.Price, buyOffer.Resources, buyOffer.ResourceType);
        if (offer is SellOffer sellOffer)
            return new(sellOffer.Seller, null, sellOffer.Price, sellOffer.Resources, sellOffer.ResourceType);
        if (offer is null)
            return default;
        throw new NotImplementedException("Unknown Offer type: " + offer.GetType().Name);
    }

    private static readonly Random _random = new();

    /// <summary>
    /// Randomly shuffle the order of decisions to ensure fairness in processing and avoid bias based on agent order.
    /// </summary>
    private static List<(ICommercialAgent Agent, Decision Decision)> Shuffle(List<(ICommercialAgent Agent, Decision Decision)> decisions)
    {
        return [.. decisions.OrderBy(_ => _random.Next())];
    }
}
