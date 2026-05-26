using Wrecs.Core;

namespace Wrecs.Systems.Commercial.Agents;

public sealed class SpreadSniperAgent(
    int minProfitPerUnit,
    int maxInventory,
    int minCashReserve) : ICommercialAgent
{
    public int Id { get; } = EntityId.Next();

    public string Name => "SpreadSniper";

    public Intent GetIntent(CommercialSnapshot state, List<Offer> offers)
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
                return new(new TakeOfferDecision(bestSell));
            }
        }

        if (bestBuy is not null
            && state.ResourceBalance >= bestBuy.Resources
            && bestSell is not null
            && OfferMath.UnitPrice(bestBuy) >= OfferMath.UnitPrice(bestSell) + minProfitPerUnit)
        {
            return new(new TakeOfferDecision(bestBuy));
        }

        if (bestSell is not null
            && state.ResourceBalance < maxInventory
            && state.MoneyBalance - bestSell.Price >= minCashReserve
            && bestBuy is not null)
        {
            var targetResaleUnit = OfferMath.UnitPrice(bestBuy) - minProfitPerUnit;

            if (OfferMath.UnitPrice(bestSell) < targetResaleUnit)
            {
                return new(new TakeOfferDecision(bestSell));
            }
        }

        if (bestBuy is not null
            && state.ResourceBalance > 0
            && bestSell is not null)
        {
            var targetAcquireUnit = OfferMath.UnitPrice(bestSell) + minProfitPerUnit;

            if (OfferMath.UnitPrice(bestBuy) > targetAcquireUnit)
            {
                return new(new TakeOfferDecision(bestBuy));
            }
        }

        if (bestSell is not null
            && bestBuy is not null
            && state.MoneyBalance > minCashReserve
            && state.ResourceBalance < maxInventory)
        {
            var bridgeBid = Math.Max(1, (bestSell.Price + bestBuy.Price) / 2 - 1);
            return new(new MakeOfferDecision(new BuyOffer(this, bridgeBid, 1)));
        }

        if (bestSell is not null
            && bestBuy is not null
            && state.ResourceBalance > 0)
        {
            var bridgeAsk = Math.Max(1, (bestSell.Price + bestBuy.Price) / 2 + 1);
            return new(new MakeOfferDecision(new SellOffer(this, bridgeAsk, 1)));
        }

        return new(new DoNothingDecision());
    }
}
