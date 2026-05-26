
namespace Wrecs.Tests;

/// <summary>
/// Agent that always does nothing.
/// </summary>
class DoNothingAgent : ICommercialAgent
{
    public int Id { get; } = EntityId.Next();
    public string Name => nameof(DoNothingAgent);

    public IEnumerable<Type> GetRequiredSnapshots() => [typeof(CommercialSnapshot)];
    public Intent GetIntent(IAgentContext context) => new Intent(new DoNothingDecision());
}

/// <summary>
/// Always buys the first sell offer it sees, or does nothing if there are no sell offers.
/// </summary>
class AlwaysBuyingTaker : ICommercialAgent
{
    public int Id { get; } = EntityId.Next();

    public string Name => nameof(AlwaysBuyingTaker);

    public IEnumerable<Type> GetRequiredSnapshots() => [typeof(CommercialSnapshot)];
    public Intent GetIntent(IAgentContext context)
    {
        var offers = context.Get<List<Offer>>();
        var sellOffer = offers.OfType<SellOffer>().FirstOrDefault();
        if (sellOffer is not null)
            return new(new TakeOfferDecision(sellOffer));
        return new(new DoNothingDecision());
    }
}

/// <summary>
/// Always sells to the first buy offer it sees, or does nothing if there are no buy offers.
/// </summary>
class AlwaysSellingTaker : ICommercialAgent
{
    public int Id { get; } = EntityId.Next();

    public string Name => nameof(AlwaysSellingTaker);

    public IEnumerable<Type> GetRequiredSnapshots() => [typeof(CommercialSnapshot)];
    public Intent GetIntent(IAgentContext context)
    {
        var offers = context.Get<List<Offer>>();
        var buyOffer = offers.OfType<BuyOffer>().FirstOrDefault();
        if (buyOffer is not null)
            return new(new TakeOfferDecision(buyOffer));
        return new(new DoNothingDecision());
    }
}

/// <summary>
/// Always makes sell offers at a fixed price and quantity each tick.
/// </summary>
class AlwaysSellingMaker(int price, int resources) : ICommercialAgent
{
    public int Id { get; } = EntityId.Next();

    public string Name => nameof(AlwaysSellingMaker);

    public IEnumerable<Type> GetRequiredSnapshots() => [typeof(CommercialSnapshot)];
    public Intent GetIntent(IAgentContext context)
    {
        return new(new MakeOfferDecision(new SellOffer(this, Price: price, Resources: resources)));
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

    public IEnumerable<Type> GetRequiredSnapshots() => [typeof(CommercialSnapshot)];
    public Intent GetIntent(IAgentContext context)
    {
        if (_hasMadeOffer)
            return new(new DoNothingDecision());
        _hasMadeOffer = true;
        return new(new MakeOfferDecision(new SellOffer(this, Price: price, Resources: resources, ResourceType: resourceType)));
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

    public IEnumerable<Type> GetRequiredSnapshots() => [typeof(CommercialSnapshot)];
    public Intent GetIntent(IAgentContext context)
    {
        if (_hasMadeOffer)
            return new(new DoNothingDecision());
        _hasMadeOffer = true;
        return new(new MakeOfferDecision(new BuyOffer(this, Price: price, Resources: resources)));
    }
}

/// <summary>
/// Offers to sell all of its resources at a fixed price each tick. If it has no resources, does nothing.
/// </summary>
class OffersToSellAllResourcesAgent(int price) : ICommercialAgent
{
    public int Id { get; } = EntityId.Next();

    public string Name => nameof(OffersToSellAllResourcesAgent);

    public IEnumerable<Type> GetRequiredSnapshots() => [typeof(CommercialSnapshot)];
    public Intent GetIntent(IAgentContext context)
    {
        var state = context.GetSnapshot<CommercialSnapshot>();
        var offers = context.Get<List<Offer>>();
        if (state.ResourceBalance > 0)
            return new(new MakeOfferDecision(new SellOffer(this, Price: price, Resources: state.ResourceBalance)));
        return new(new DoNothingDecision());
    }
}
