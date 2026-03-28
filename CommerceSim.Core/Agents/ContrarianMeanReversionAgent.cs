namespace CommerceSim.Core.Agents;

public sealed class ContrarianMeanReversionAgent(
    int lookback = 8,
    int maxInventory = 8,
    int minCashReserve = 2,
    double zThreshold = 1.25) : IAgent
{
    private readonly Queue<double> midPrices = new();

    public string Name => "ContrarianMeanReversion";


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

        if (midPrices.Count < 4)
        {
            return new DoNothingDecision();
        }

        var prices = midPrices.ToArray();
        var mean = prices.Average();
        var variance = prices.Select(x => (x - mean) * (x - mean)).Average();
        var stdDev = Math.Sqrt(Math.Max(variance, 0.0001));
        var zScore = (prices[^1] - mean) / stdDev;

        var bestSell = offers
            .OfType<SellOffer>()
            .Where(x => x.Seller != this)
            .OrderBy(x => OfferMath.UnitPrice(x))
            .FirstOrDefault();

        if (zScore <= -zThreshold
            && bestSell is not null
            && state.ResourceBalance < maxInventory
            && state.MoneyBalance - bestSell.Price >= minCashReserve
            && OfferMath.UnitPrice(bestSell) <= mean)
        {
            return new TakeOfferDecision(bestSell);
        }

        var bestBuy = offers
            .OfType<BuyOffer>()
            .Where(x => x.Buyer != this)
            .OrderByDescending(x => OfferMath.UnitPrice(x))
            .FirstOrDefault();

        if (zScore >= zThreshold
            && bestBuy is not null
            && state.ResourceBalance >= bestBuy.Resources
            && OfferMath.UnitPrice(bestBuy) >= mean)
        {
            return new TakeOfferDecision(bestBuy);
        }

        if (zScore > 0.5 && state.ResourceBalance > 0)
        {
            return new MakeOfferDecision(new SellOffer(this, Math.Max(1, (int)Math.Ceiling(mean)), 1));
        }

        if (zScore < -0.5 && state.MoneyBalance > minCashReserve + mean)
        {
            return new MakeOfferDecision(new BuyOffer(this, Math.Max(1, (int)Math.Floor(mean)), 1));
        }

        return new DoNothingDecision();
    }
}
