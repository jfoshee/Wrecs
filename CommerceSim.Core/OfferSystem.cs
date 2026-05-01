namespace CommerceSim.Core;

public record struct OfferSnapshot(ICommercialAgent? Seller, ICommercialAgent? Buyer, int Price, int Resources, string? ResourceType);

public record struct OfferListSnapshot(List<OfferSnapshot> OfferSnapshots);

public class OfferSystem : ISystem<ICommercialAgent, OfferListSnapshot>, IRequire<CommercialSystem>
{
    private readonly List<IEntity> _entities = [];
    private readonly Dictionary<IEntity, List<Offer>> _stateMap = [];
    private CommercialSystem? _commercialSystem;

    public void Inject(CommercialSystem dependency) => _commercialSystem = dependency;

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

    public OfferListSnapshot GetState(IEntity entity)
    {
        var state = _stateMap.GetValueOrDefault(entity);
        return Snapshot(state);
    }

    public void SetStates(IEnumerable<(IEntity entity, OfferListSnapshot state)> stateUpdates)
    {
        foreach (var (entity, state) in stateUpdates)
        {
            _stateMap[entity] = State(state);
        }
    }

    public void Tick()
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
            var commercialState = _commercialSystem?.GetState(agent) ?? throw new InvalidOperationException("No state for agent");
            var decision = agent.Decide(commercialState, offersForAgent);
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
                    // allOffers.Remove(offer);
                    ProcessOffer(takeOfferDecision, _states[offer.Author], _states[agent]);
                    break;
                case MakeOfferDecision makeOfferDecision:
                    var newOffer = makeOfferDecision.Offer;
                    AddOffer(agent, newOffer);
                    break;
            }
        }
    }

    private void AddOffer(ICommercialAgent agent, Offer offer)
    {
        if (!_stateMap.ContainsKey(agent))
            _stateMap[agent] = [];
        _stateMap[agent].Add(offer);
    }

    private static List<Offer> State(OfferListSnapshot snapshot)
    {
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
