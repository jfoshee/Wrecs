using Wrecs.Core;

namespace Wrecs.Systems.Commercial.Agents;

public sealed class RandomAgent(int maxPrice, Random? random = null) : ICommercialAgent
{
    private readonly Random _random = random ?? Random.Shared;
    public int Id { get; } = EntityId.Next();

    public string Name => "Random " + Id;

    public IEnumerable<Type> GetRequiredSnapshots() => [typeof(CommercialSnapshot), typeof(OfferListSnapshot)];
    public Intent GetIntent(IAgentContext context)
    {
        var state = context.GetSnapshot<CommercialSnapshot>();
        var offers = context.GetSnapshot<OfferListSnapshot>().Offers?.ToList() ?? [];
        // Randomly choose an action: 0 = do nothing, 1 = take offer, 2 = make buy offer, 3 = make sell offer
        var action = _random.Next(4);

        return action switch
        {
            1 => TryTakeRandomOffer(state, offers),
            2 => TryMakeBuyOffer(state),
            3 => TryMakeSellOffer(state),
            _ => new Intent(new DoNothingDecision()),
        };
    }

    private Intent TryTakeRandomOffer(CommercialSnapshot state, List<Offer> offers)
    {
        var availableOffers = offers
            .Where(o => o.Author != this && !o.Used)
            .ToList();

        if (availableOffers.Count == 0)
            return new(new DoNothingDecision());

        var offer = availableOffers[_random.Next(availableOffers.Count)];

        // Check if we can afford/fulfill the offer
        if (offer is SellOffer && state.MoneyBalance >= offer.Price)
            return new(new TakeOfferDecision(offer));

        if (offer is BuyOffer && state.ResourceBalance >= offer.Resources)
            return new(new TakeOfferDecision(offer));

        return new(new DoNothingDecision());
    }

    private Intent TryMakeBuyOffer(CommercialSnapshot state)
    {
        var price = _random.Next(1, maxPrice + 1);
        if (state.MoneyBalance >= price)
            return new(new MakeOfferDecision(new BuyOffer(this, price, 1)));

        return new(new DoNothingDecision());
    }

    private Intent TryMakeSellOffer(CommercialSnapshot state)
    {
        if (state.ResourceBalance <= 0)
            return new(new DoNothingDecision());

        var price = _random.Next(1, maxPrice + 1);
        return new(new MakeOfferDecision(new SellOffer(this, price, 1)));
    }
}
