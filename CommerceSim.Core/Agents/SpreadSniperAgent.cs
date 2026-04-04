namespace CommerceSim.Core.Agents;

public sealed class SpreadSniperAgent(
    int minProfitPerUnit,
    int maxInventory,
    int minCashReserve) : IAgent
{
    private readonly int _id = AgentId.Next();
    public int Id => _id;

    public string Name => "SpreadSniper";

    public Decision Decide(AgentStateSnapshot state, List<Offer> offers)
    {
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

        if (bestSell is not null
            && bestBuy is not null
            && bestBuy.Resources >= bestSell.Resources
            && OfferMath.UnitPrice(bestBuy) - OfferMath.UnitPrice(bestSell) >= minProfitPerUnit)
        {
            if (state.MoneyBalance - bestSell.Price >= minCashReserve)
            {
                return new TakeOfferDecision(bestSell);
            }
        }

        if (bestBuy is not null
            && state.ResourceBalance >= bestBuy.Resources
            && bestSell is not null
            && OfferMath.UnitPrice(bestBuy) >= OfferMath.UnitPrice(bestSell) + minProfitPerUnit)
        {
            return new TakeOfferDecision(bestBuy);
        }

        if (bestSell is not null
            && state.ResourceBalance < maxInventory
            && state.MoneyBalance - bestSell.Price >= minCashReserve
            && bestBuy is not null)
        {
            var targetResaleUnit = OfferMath.UnitPrice(bestBuy) - minProfitPerUnit;

            if (OfferMath.UnitPrice(bestSell) < targetResaleUnit)
            {
                return new TakeOfferDecision(bestSell);
            }
        }

        if (bestBuy is not null
            && state.ResourceBalance > 0
            && bestSell is not null)
        {
            var targetAcquireUnit = OfferMath.UnitPrice(bestSell) + minProfitPerUnit;

            if (OfferMath.UnitPrice(bestBuy) > targetAcquireUnit)
            {
                return new TakeOfferDecision(bestBuy);
            }
        }

        if (bestSell is not null
            && bestBuy is not null
            && state.MoneyBalance > minCashReserve
            && state.ResourceBalance < maxInventory)
        {
            var bridgeBid = Math.Max(1, (bestSell.Price + bestBuy.Price) / 2 - 1);
            return new MakeOfferDecision(new BuyOffer(this, bridgeBid, 1));
        }

        if (bestSell is not null
            && bestBuy is not null
            && state.ResourceBalance > 0)
        {
            var bridgeAsk = Math.Max(1, (bestSell.Price + bestBuy.Price) / 2 + 1);
            return new MakeOfferDecision(new SellOffer(this, bridgeAsk, 1));
        }

        return new DoNothingDecision();
    }
}
