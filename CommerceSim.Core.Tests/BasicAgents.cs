namespace CommerceSim.Core.Tests;

/// <summary>
/// Always buys the first sell offer it sees, or does nothing if there are no sell offers.
/// </summary>
class AlwaysBuyingAgent : Agent
{
    public override Decision Decide(AgentStateSnapshot _, List<Offer> opportunities)
    {
        var sellOffer = opportunities.OfType<SellOffer>().FirstOrDefault();
        if (sellOffer is not null)
            return new TakeOfferDecision(sellOffer);
        return new DoNothingDecision();
    }
}

/// <summary>
/// Always sells to the first buy offer it sees, or does nothing if there are no buy offers.
/// </summary>
class AlwaysSellingAgent : Agent
{
    public override Decision Decide(AgentStateSnapshot _, List<Offer> opportunities)
    {
        var buyOffer = opportunities.OfType<BuyOffer>().FirstOrDefault();
        if (buyOffer is not null)
            return new TakeOfferDecision(buyOffer);
        return new DoNothingDecision();
    }
}

/// <summary>
/// Makes a single sell offer on the first tick, then does nothing on subsequent ticks.
/// </summary>
class MakesSellOfferAgent(int price, int resources) : Agent
{
    private bool _hasMadeOffer = false;

    public override Decision Decide(AgentStateSnapshot _, List<Offer> opportunities)
    {
        if (_hasMadeOffer)
            return new DoNothingDecision();
        _hasMadeOffer = true;
        return new MakeOfferDecision(new SellOffer(this, Price: price, Resources: resources));
    }
}
