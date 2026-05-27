using Wrecs.Core;

namespace Wrecs.Systems.Commercial;

// TODO: enforce that offers in a snapshot cannot be mutated (so consumers can't change internal System state)
public record struct OfferListSnapshot(IReadOnlyList<Offer>? Offers) : IStateSnapshot<OfferSystem>
{
    public override readonly string ToString()
    {
        // When this struct is defaulted Offers is null
        var snapshots = Offers ?? [];
        return "[" + string.Join(", ", snapshots.Select(x => x.ToString())) + "]";
    }
}

public record struct RemoveOfferOperation(Offer? Offer) : IStateSnapshot<OfferSystem>;
public record struct AddOfferOperation(Offer Offer) : IStateSnapshot<OfferSystem>;

public class OfferSystem :
    ISystem<ICommercialAgent, OfferListSnapshot>,
    IBuildAgentContext,
    ITranslateIntent<TakeOfferDecision>,
    ITranslateIntent<MakeOfferDecision>,
    IAcceptUpdates<RemoveOfferOperation>,
    IAcceptUpdates<AddOfferOperation>,
    IRequire<MoneySystem>,
    IRequire<InventorySystem>
{
    private readonly List<IEntity> _entities = [];
    private readonly Dictionary<IEntity, List<Offer>> _stateMap = [];
    private MoneySystem? _moneySystem;
    private InventorySystem? _inventorySystem;
    private List<Offer>? _cachedAllOffers;

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
        // Cache all current offers for use during centralized agent invocation (PopulateContext)
        _cachedAllOffers = _stateMap.Values.SelectMany(x => x).ToList();
    }

    public void ApplyInternalUpdates()
    {
        _cachedAllOffers = null;
    }

    public void PopulateContext(IAgent agent, AgentContext context)
    {
        // TODO: use agent.GetRequiredSnapshots
        if (agent is not ICommercialAgent commercialAgent)
            return;
        var allOffers = _cachedAllOffers ?? [];
        // Filter offers: include general offers + targeted offers for this agent
        var offersForAgent = allOffers
            .Where(o => o is not TargetedSellOffer targeted || targeted.Buyer == commercialAgent)
            .ToList();
        context.AddSnapshot(BuildCommercialSnapshot(commercialAgent));
        context.AddSnapshot(new OfferListSnapshot(offersForAgent));
    }

    public UpdateSet TranslateIntent(IAgent agent, TakeOfferDecision action)
    {
        if (agent is not ICommercialAgent commercialAgent)
            return new UpdateSet([]);
        return ProcessOffer(commercialAgent, action.Offer);
    }

    public UpdateSet TranslateIntent(IAgent agent, MakeOfferDecision action)
    {
        if (agent is not ICommercialAgent commercialAgent)
            return new UpdateSet([]);
        return new UpdateSet([new EntityUpdate<AddOfferOperation>(commercialAgent, new(action.Offer))]);
    }

    // Explicit implementations required because OfferSystem implements ITranslateIntent<T> twice
    bool ITranslateIntent.CanTranslate(IIntentAction action) =>
        action is TakeOfferDecision || action is MakeOfferDecision;

    UpdateSet ITranslateIntent.Translate(IAgent agent, IIntentAction action) => action switch
    {
        TakeOfferDecision take => TranslateIntent(agent, take),
        MakeOfferDecision make => TranslateIntent(agent, make),
        _ => new UpdateSet([])
    };

    public void ApplyUpdates(IEnumerable<EntityUpdate<RemoveOfferOperation>> updates)
    {
        foreach (var update in updates)
        {
            var operation = update.State;
            if (operation.Offer is not null)
                RemoveOffer(operation.Offer.Author, operation.Offer);
        }
    }

    public void ApplyUpdates(IEnumerable<EntityUpdate<AddOfferOperation>> updates)
    {
        foreach (var update in updates)
            AddOffer(update.State.Offer.Author, update.State.Offer);
    }

    // Explicit implementation required because OfferSystem implements IAcceptUpdates<T> twice
    void IAcceptUpdates.ApplyUpdates(IEnumerable<IEntityUpdate> updates)
    {
        var all = updates.ToList();
        ApplyUpdates(all.OfType<EntityUpdate<RemoveOfferOperation>>());
        ApplyUpdates(all.OfType<EntityUpdate<AddOfferOperation>>());
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
        if (snapshot.Offers is null)
            return [];
        return snapshot.Offers.ToList();
    }

    private static OfferListSnapshot Snapshot(List<Offer>? offers)
    {
        if (offers is null)
            return default;
        return new(offers);
    }

}
