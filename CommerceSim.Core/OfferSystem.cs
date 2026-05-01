namespace CommerceSim.Core;

public class OfferSystem : ISystem<ICommercialAgent, OfferSnapshot>
{
    private readonly List<IEntity> _entities = [];
    private readonly Dictionary<IEntity, Offer> _stateMap = [];

    public void InitEntities(params (IEntity entity, OfferSnapshot? initialState)[] initialEntities)
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

    public OfferSnapshot GetState(IEntity entity)
    {
        var state = _stateMap.GetValueOrDefault(entity);
        return Snapshot(state);
    }

    public void SetStates(IEnumerable<(IEntity entity, OfferSnapshot state)> stateUpdates)
    {
        foreach (var (entity, state) in stateUpdates)
        {
            _stateMap[entity] = State(state);
        }
    }

    public void Tick()
    {
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

    private static OfferSnapshot Snapshot(Offer? offer)
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
}
