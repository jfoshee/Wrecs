namespace CommerceSim.Core.Agents;

public sealed class MomentumChaserAgent(
    int lookback = 6,
    int maxInventory = 6,
    int minCashReserve = 2) : ICommerceAgent
{
    private readonly int _id = EntityId.Next();
    public int Id => _id;
    private readonly Queue<double> midPrices = new();

    public string Name => "MomentumChaser";

    public Decision Decide(AgentStateSnapshot state, List<Offer> offers)
    {
        var market = OfferMath.GetMarketSnapshot(offers, this);

        if (market.MidPrice is double mid)
        {
            midPrices.Enqueue(mid);

            while (midPrices.Count > lookback)
            {
                midPrices.Dequeue();
            }
        }

        if (midPrices.Count < 3)
        {
            return PassiveQuote(state, market);
        }

        var prices = midPrices.ToArray();
        var trend = prices[^1] - prices[0];

        var bestSell = offers
            .OfType<SellOffer>()
            .Where(x => x.Seller != this)
            .OrderBy(x => OfferMath.UnitPrice(x))
            .FirstOrDefault();

        var bestBuy = offers
            .OfType<BuyOffer>()
            .Where(x => x.Buyer != this)
            .OrderByDescending(x => OfferMath.UnitPrice(x))
            .FirstOrDefault();

        if (trend > 1.0
            && bestSell is not null
            && state.ResourceBalance < maxInventory
            && state.MoneyBalance - bestSell.Price >= minCashReserve)
        {
            return new TakeOfferDecision(bestSell);
        }

        if (trend < -1.0
            && bestBuy is not null
            && state.ResourceBalance >= bestBuy.Resources)
        {
            return new TakeOfferDecision(bestBuy);
        }

        return PassiveQuote(state, market);
    }

    private Decision PassiveQuote(AgentStateSnapshot state, OfferMath.MarketSnapshot market)
    {
        var mid = market.MidPrice ?? 5.0;

        if (state.ResourceBalance > 0)
        {
            return new MakeOfferDecision(new SellOffer(this, Math.Max(1, (int)Math.Ceiling(mid + 1)), 1));
        }

        if (state.MoneyBalance > minCashReserve + mid)
        {
            return new MakeOfferDecision(new BuyOffer(this, Math.Max(1, (int)Math.Floor(mid - 1)), 1));
        }

        return new DoNothingDecision();
    }
}
