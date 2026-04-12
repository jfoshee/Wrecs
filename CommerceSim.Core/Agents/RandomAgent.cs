namespace CommerceSim.Core.Agents;

public sealed class RandomAgent(int maxPrice, Random? random = null) : ICommercialAgent
{
    private readonly Random _random = random ?? Random.Shared;
    public int Id { get; } = EntityId.Next();

    public string Name => "Random " + Id;

    public Decision Decide(CommercialSnapshot state, List<Offer> offers)
    {
        // Randomly choose an action: 0 = do nothing, 1 = take offer, 2 = make buy offer, 3 = make sell offer
        var action = _random.Next(4);

        return action switch
        {
            1 => TryTakeRandomOffer(state, offers),
            2 => TryMakeBuyOffer(state),
            3 => TryMakeSellOffer(state),
            _ => new DoNothingDecision(),
        };
    }

    private Decision TryTakeRandomOffer(CommercialSnapshot state, List<Offer> offers)
    {
        var availableOffers = offers
            .Where(o => o.Author != this && !o.Used)
            .ToList();

        if (availableOffers.Count == 0)
            return new DoNothingDecision();

        var offer = availableOffers[_random.Next(availableOffers.Count)];

        // Check if we can afford/fulfill the offer
        if (offer is SellOffer && state.MoneyBalance >= offer.Price)
            return new TakeOfferDecision(offer);

        if (offer is BuyOffer && state.ResourceBalance >= offer.Resources)
            return new TakeOfferDecision(offer);

        return new DoNothingDecision();
    }

    private Decision TryMakeBuyOffer(CommercialSnapshot state)
    {
        var price = _random.Next(1, maxPrice + 1);
        if (state.MoneyBalance >= price)
            return new MakeOfferDecision(new BuyOffer(this, price, 1));

        return new DoNothingDecision();
    }

    private Decision TryMakeSellOffer(CommercialSnapshot state)
    {
        if (state.ResourceBalance <= 0)
            return new DoNothingDecision();

        var price = _random.Next(1, maxPrice + 1);
        return new MakeOfferDecision(new SellOffer(this, price, 1));
    }
}
