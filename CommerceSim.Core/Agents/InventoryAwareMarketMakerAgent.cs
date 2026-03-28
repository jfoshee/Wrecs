namespace CommerceSim.Core.Agents;

public sealed class InventoryAwareMarketMakerAgent(
    int anchorPrice,
    int targetInventory,
    int inventoryTolerance,
    int minSpread = 2) : IAgent
{
    private readonly int _id = AgentId.Next();
    public string Name => "Inventory-Aware Market-Maker " + _id;

    public Decision Decide(AgentStateSnapshot state, List<Offer> offers)
    {
        var market = OfferMath.GetMarketSnapshot(offers, this);
        var mid = market.MidPrice ?? anchorPrice;

        var inventorySkew = state.ResourceBalance - targetInventory;
        var spread = Math.Max(minSpread, 2 + Math.Abs(inventorySkew));

        var bidUnit = Math.Max(1, mid - spread - Math.Max(0, inventorySkew));
        var askUnit = Math.Max(bidUnit + 1, mid + spread - Math.Min(0, inventorySkew));

        var attractiveSell = offers
            .OfType<SellOffer>()
            .Where(x => x.Seller != this)
            .OrderBy(x => OfferMath.UnitPrice(x))
            .FirstOrDefault();

        if (attractiveSell is not null
            && state.MoneyBalance >= attractiveSell.Price
            && state.ResourceBalance < targetInventory + inventoryTolerance
            && OfferMath.UnitPrice(attractiveSell) <= bidUnit)
        {
            return new TakeOfferDecision(attractiveSell);
        }

        var attractiveBuy = offers
            .OfType<BuyOffer>()
            .Where(x => x.Buyer != this)
            .OrderByDescending(x => OfferMath.UnitPrice(x))
            .FirstOrDefault();

        if (attractiveBuy is not null
            && state.ResourceBalance >= attractiveBuy.Resources
            && state.ResourceBalance > targetInventory - inventoryTolerance
            && OfferMath.UnitPrice(attractiveBuy) >= askUnit)
        {
            return new TakeOfferDecision(attractiveBuy);
        }

        if (state.ResourceBalance > targetInventory + inventoryTolerance)
        {
            return new MakeOfferDecision(new SellOffer(this, (int)askUnit, 1));
        }

        if (state.MoneyBalance >= bidUnit && state.ResourceBalance < targetInventory - inventoryTolerance)
        {
            return new MakeOfferDecision(new BuyOffer(this, (int)bidUnit, 1));
        }

        if (state.MoneyBalance >= bidUnit && state.ResourceBalance <= targetInventory)
        {
            return new MakeOfferDecision(new BuyOffer(this, (int)bidUnit, 1));
        }

        if (state.ResourceBalance > 0)
        {
            return new MakeOfferDecision(new SellOffer(this, (int)askUnit, 1));
        }

        return new DoNothingDecision();
    }
}
