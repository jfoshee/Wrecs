namespace CommerceSim.Core.Agents;

public sealed class ValueInvestorAgent(
    int initialFairPrice,
    int maxPosition,
    int minCashReserve,
    double adjustmentRate = 0.15) : IAgent
{
    private double fairPrice = initialFairPrice;

    public string Name => "ValueInvestor";

    public Decision Decide(AgentStateSnapshot state, List<Offer> offers)
    {
        UpdateFairPrice(offers);

        var bestSell = offers
            .OfType<SellOffer>()
            .Where(x => x.Seller != this)
            .OrderBy(x => x.Price)
            .FirstOrDefault();

        if (bestSell is not null
            && state.ResourceBalance < maxPosition
            && state.MoneyBalance - bestSell.Price >= minCashReserve
            && UnitPrice(bestSell) <= fairPrice * 0.9)
        {
            return new TakeOfferDecision(bestSell);
        }

        var bestBuy = offers
            .OfType<BuyOffer>()
            .Where(x => x.Buyer != this)
            .OrderByDescending(x => x.Price)
            .FirstOrDefault();

        if (bestBuy is not null
            && state.ResourceBalance >= bestBuy.Resources
            && UnitPrice(bestBuy) >= fairPrice * 1.1)
        {
            return new TakeOfferDecision(bestBuy);
        }

        if (state.ResourceBalance > 0)
        {
            var askPrice = Math.Max(1, (int)Math.Ceiling(fairPrice * 1.08));
            return new MakeOfferDecision(new SellOffer(this, askPrice, 1));
        }

        if (state.MoneyBalance > minCashReserve + Math.Max(1, (int)Math.Floor(fairPrice)))
        {
            var bidPrice = Math.Max(1, (int)Math.Floor(fairPrice * 0.92));
            return new MakeOfferDecision(new BuyOffer(this, bidPrice, 1));
        }

        return new DoNothingDecision();
    }

    private void UpdateFairPrice(List<Offer> offers)
    {
        var pricedOffers = offers
            .Where(x => x.Author != this && x.Resources > 0)
            .Select(UnitPrice)
            .ToArray();

        if (pricedOffers.Length == 0)
        {
            return;
        }

        var average = pricedOffers.Average();
        fairPrice = (fairPrice * (1.0 - adjustmentRate)) + (average * adjustmentRate);
    }

    private static double UnitPrice(Offer offer) => (double)offer.Price / offer.Resources;
}
