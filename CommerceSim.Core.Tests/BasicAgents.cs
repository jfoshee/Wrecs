using CommerceSim.Core.Agents;

namespace CommerceSim.Core.Tests;

/// <summary>
/// Agent that always does nothing.
/// </summary>
class DoNothingAgent : ICommercialAgent
{
    public int Id { get; } = EntityId.Next();
    public string Name => nameof(DoNothingAgent);

    public Decision Decide(CommercialSnapshot _, List<Offer> opportunities) => new DoNothingDecision();
}

/// <summary>
/// Always buys the first sell offer it sees, or does nothing if there are no sell offers.
/// </summary>
class AlwaysBuyingTaker : ICommercialAgent
{
    public int Id { get; } = EntityId.Next();

    public string Name => nameof(AlwaysBuyingTaker);

    public Decision Decide(CommercialSnapshot _, List<Offer> opportunities)
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
class AlwaysSellingTaker : ICommercialAgent
{
    public int Id { get; } = EntityId.Next();

    public string Name => nameof(AlwaysSellingTaker);

    public Decision Decide(CommercialSnapshot _, List<Offer> opportunities)
    {
        var buyOffer = opportunities.OfType<BuyOffer>().FirstOrDefault();
        if (buyOffer is not null)
            return new TakeOfferDecision(buyOffer);
        return new DoNothingDecision();
    }
}

/// <summary>
/// Always makes sell offers at a fixed price and quantity each tick.
/// </summary>
class AlwaysSellingMaker(int price, int resources) : ICommercialAgent
{
    public int Id { get; } = EntityId.Next();

    public string Name => nameof(AlwaysSellingMaker);

    public Decision Decide(CommercialSnapshot _, List<Offer> opportunities)
    {
        return new MakeOfferDecision(new SellOffer(this, Price: price, Resources: resources));
    }
}

/// <summary>
/// Makes a single sell offer on the first tick, then does nothing on subsequent ticks.
/// </summary>
class MakesSellOfferAgent(int price, int resources, string? resourceType = null) : ICommercialAgent
{
    public int Id { get; } = EntityId.Next();
    private bool _hasMadeOffer = false;

    public string Name => nameof(MakesSellOfferAgent);

    public Decision Decide(CommercialSnapshot _, List<Offer> opportunities)
    {
        if (_hasMadeOffer)
            return new DoNothingDecision();
        _hasMadeOffer = true;
        return new MakeOfferDecision(new SellOffer(this, Price: price, Resources: resources, ResourceType: resourceType));
    }
}

/// <summary>
/// Makes a single buy offer on the first tick, then does nothing on subsequent ticks.
/// </summary>
class MakesBuyOfferAgent(int price, int resources) : ICommercialAgent
{
    public int Id { get; } = EntityId.Next();
    private bool _hasMadeOffer = false;

    public string Name => nameof(MakesBuyOfferAgent);

    public Decision Decide(CommercialSnapshot _, List<Offer> opportunities)
    {
        if (_hasMadeOffer)
            return new DoNothingDecision();
        _hasMadeOffer = true;
        return new MakeOfferDecision(new BuyOffer(this, Price: price, Resources: resources));
    }
}

/// <summary>
/// Offers to sell all of its resources at a fixed price each tick. If it has no resources, does nothing.
/// </summary>
class OffersToSellAllResourcesAgent(int price) : ICommercialAgent
{
    public int Id { get; } = EntityId.Next();

    public string Name => nameof(OffersToSellAllResourcesAgent);

    public Decision Decide(CommercialSnapshot state, List<Offer> opportunities)
    {
        if (state.ResourceBalance > 0)
            return new MakeOfferDecision(new SellOffer(this, Price: price, Resources: state.ResourceBalance));
        return new DoNothingDecision();
    }
}
