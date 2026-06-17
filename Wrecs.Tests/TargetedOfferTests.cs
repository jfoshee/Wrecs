namespace Wrecs.Tests;

#region Test Agents

/// <summary>
/// Makes a targeted sell offer to a specific buyer on the first tick.
/// </summary>
class MakesTargetedSellOfferAgent(ICommercialAgent target, int price, int resources) : ICommercialAgent
{
    public int Id { get; } = EntityId.Next();
    public string Name => nameof(MakesTargetedSellOfferAgent);
    private bool _hasMadeOffer = false;

    public Intent GetIntent(IAgentContext context)
    {
        if (_hasMadeOffer)
            return new(new DoNothingDecision());
        _hasMadeOffer = true;
        return new(new MakeOfferDecision(new TargetedSellOffer(this, target, price, resources)));
    }
}

/// <summary>
/// Tracks all offers seen and takes the first targeted sell offer aimed at it.
/// </summary>
class TargetedOfferReceiverAgent : ICommercialAgent
{
    public int Id { get; } = EntityId.Next();
    public string Name => nameof(TargetedOfferReceiverAgent);

    public List<List<Offer>> OffersSeenPerTick { get; } = [];

    public Intent GetIntent(IAgentContext context)
    {
        var opportunities = context.GetSnapshot<OfferListSnapshot>().Offers?.ToList() ?? [];
        OffersSeenPerTick.Add([.. opportunities]);
        var targetedOffer = opportunities.OfType<TargetedSellOffer>()
            .FirstOrDefault(o => o.Buyer == this);
        if (targetedOffer is not null)
            return new(new TakeOfferDecision(targetedOffer));
        return new(new DoNothingDecision());
    }
}

/// <summary>
/// Tracks all offers seen but never takes any action.
/// </summary>
class OfferObserverAgent : ICommercialAgent
{
    public int Id { get; } = EntityId.Next();
    public string Name { get; init; } = nameof(OfferObserverAgent);

    public List<List<Offer>> OffersSeenPerTick { get; } = [];

    public Intent GetIntent(IAgentContext context)
    {
        var opportunities = context.GetSnapshot<OfferListSnapshot>().Offers?.ToList() ?? [];
        OffersSeenPerTick.Add([.. opportunities]);
        return new(new DoNothingDecision());
    }
}

/// <summary>
/// Makes a general sell offer on tick 1, then a targeted sell offer on tick 2.
/// </summary>
class MakesGeneralAndTargetedOffersAgent(
    ICommercialAgent target,
    int generalPrice, int generalResources,
    int targetedPrice, int targetedResources) : ICommercialAgent
{
    public int Id { get; } = EntityId.Next();
    public string Name => nameof(MakesGeneralAndTargetedOffersAgent);
    private int _tickCount = 0;

    public Intent GetIntent(IAgentContext context)
    {
        _tickCount++;
        return _tickCount switch
        {
            1 => new Intent(new MakeOfferDecision(new SellOffer(this, generalPrice, generalResources))),
            2 => new Intent(new MakeOfferDecision(new TargetedSellOffer(this, target, targetedPrice, targetedResources))),
            _ => new Intent(new DoNothingDecision())
        };
    }
}

#endregion

public class TargetedOfferTests
{
    [Fact]
    public void BasicTargetedOffer_TargetSeesAndTakesOffer()
    {
        // Arrange
        var receiver = new TargetedOfferReceiverAgent();
        var seller = new MakesTargetedSellOfferAgent(receiver, price: 10, resources: 5);
        var sim = new CommercialSimHarness();
        sim.InitEntities(
            (seller, new CommercialSnapshot(MoneyBalance: 0, ResourceBalance: 100)),
            (receiver, new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 0)));

        // Act - Tick 1: seller makes targeted offer
        sim.Tick();
        // Act - Tick 2: receiver sees and takes the offer
        sim.Tick();

        // Assert - receiver should have seen the offer on tick 2 and taken it
        var receiverState = sim.GetCommercialState(receiver);
        var sellerState = sim.GetCommercialState(seller);

        Assert.Equal(5, receiverState.ResourceBalance); // Received 5 resources
        Assert.Equal(90, receiverState.MoneyBalance);   // Paid 10 for resources
        Assert.Equal(95, sellerState.ResourceBalance);  // Sold 5 resources
        Assert.Equal(10, sellerState.MoneyBalance);     // Received 10 for resources
    }

    [Fact]
    public void TargetedOffer_NonTargetDoesNotSeeOffer()
    {
        // Arrange
        var target = new OfferObserverAgent { Name = "Target" };
        var nonTarget = new OfferObserverAgent { Name = "NonTarget" };
        var seller = new MakesTargetedSellOfferAgent(target, price: 10, resources: 5);
        var sim = new CommercialSimHarness();
        sim.InitEntities(
            (seller, new CommercialSnapshot(MoneyBalance: 0, ResourceBalance: 100)),
            (target, new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 0)),
            (nonTarget, new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 0)));

        // Act - Tick 1: seller makes targeted offer
        sim.Tick();
        // Act - Tick 2: agents observe offers
        sim.Tick();

        // Assert - target sees the offer, non-target does not
        // Tick 2 is index 1 in OffersSeenPerTick
        var targetOffersOnTick2 = target.OffersSeenPerTick[1];
        var nonTargetOffersOnTick2 = nonTarget.OffersSeenPerTick[1];

        Assert.Single(targetOffersOnTick2.OfType<TargetedSellOffer>());
        Assert.Empty(nonTargetOffersOnTick2.OfType<TargetedSellOffer>());
    }

    [Fact]
    public void MultipleTargetedOffersToSameTarget_TargetSeesAll()
    {
        // Arrange
        var receiver = new OfferObserverAgent { Name = "Receiver" };
        var seller1 = new MakesTargetedSellOfferAgent(receiver, price: 10, resources: 5);
        var seller2 = new MakesTargetedSellOfferAgent(receiver, price: 20, resources: 3);
        var sim = new CommercialSimHarness();
        sim.InitEntities(
            (seller1, new CommercialSnapshot(MoneyBalance: 0, ResourceBalance: 100)),
            (seller2, new CommercialSnapshot(MoneyBalance: 0, ResourceBalance: 100)),
            (receiver, new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 0)));

        // Act - Tick 1: both sellers make targeted offers
        sim.Tick();
        // Act - Tick 2: receiver observes offers
        sim.Tick();

        // Assert - receiver sees both targeted offers
        var receiverOffersOnTick2 = receiver.OffersSeenPerTick[1];
        var targetedOffers = receiverOffersOnTick2.OfType<TargetedSellOffer>().ToList();

        Assert.Equal(2, targetedOffers.Count);
        Assert.Contains(targetedOffers, o => o.Price == 10 && o.Resources == 5);
        Assert.Contains(targetedOffers, o => o.Price == 20 && o.Resources == 3);
    }

    [Fact]
    public void DifferentTargets_EachSeesOnlyTheirOwnOffers()
    {
        // Arrange
        var targetA = new OfferObserverAgent { Name = "TargetA" };
        var targetB = new OfferObserverAgent { Name = "TargetB" };
        var sellerA = new MakesTargetedSellOfferAgent(targetA, price: 100, resources: 1);
        var sellerB = new MakesTargetedSellOfferAgent(targetB, price: 200, resources: 2);
        var sim = new CommercialSimHarness();
        sim.InitEntities(
            (sellerA, new CommercialSnapshot(MoneyBalance: 0, ResourceBalance: 100)),
            (sellerB, new CommercialSnapshot(MoneyBalance: 0, ResourceBalance: 100)),
            (targetA, new CommercialSnapshot(MoneyBalance: 500, ResourceBalance: 0)),
            (targetB, new CommercialSnapshot(MoneyBalance: 500, ResourceBalance: 0)));

        // Act - Tick 1: sellers make targeted offers to their respective targets
        sim.Tick();
        // Act - Tick 2: targets observe offers
        sim.Tick();

        // Assert - each target sees only the offer targeted at them
        var targetAOffers = targetA.OffersSeenPerTick[1].OfType<TargetedSellOffer>().ToList();
        var targetBOffers = targetB.OffersSeenPerTick[1].OfType<TargetedSellOffer>().ToList();

        Assert.Single(targetAOffers);
        Assert.Equal(100, targetAOffers[0].Price);
        Assert.Equal(1, targetAOffers[0].Resources);
        Assert.Equal(sellerA, targetAOffers[0].Seller);

        Assert.Single(targetBOffers);
        Assert.Equal(200, targetBOffers[0].Price);
        Assert.Equal(2, targetBOffers[0].Resources);
        Assert.Equal(sellerB, targetBOffers[0].Seller);
    }

    [Fact]
    public void MixedGeneralAndTargetedOffers_TargetSeesBoth_NonTargetSeesOnlyGeneral()
    {
        // Arrange
        var target = new OfferObserverAgent { Name = "Target" };
        var nonTarget = new OfferObserverAgent { Name = "NonTarget" };
        // Agent that makes both a general and targeted offer
        var seller = new MakesGeneralAndTargetedOffersAgent(target,
            generalPrice: 15, generalResources: 2,
            targetedPrice: 25, targetedResources: 4);
        var sim = new CommercialSimHarness();
        sim.InitEntities(
            (seller, new CommercialSnapshot(MoneyBalance: 0, ResourceBalance: 100)),
            (target, new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 0)),
            (nonTarget, new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 0)));

        // Act - Tick 1: seller makes both offers (takes 2 ticks to make both)
        sim.Tick();
        sim.Tick();
        // Act - Tick 3: agents observe all offers
        sim.Tick();

        // Assert - target sees both general and targeted offers
        var targetOffers = target.OffersSeenPerTick[2];
        var targetGeneralOffers = targetOffers.OfType<SellOffer>()
            .Where(o => o is not TargetedSellOffer).ToList();
        var targetTargetedOffers = targetOffers.OfType<TargetedSellOffer>().ToList();

        Assert.Single(targetGeneralOffers);
        Assert.Equal(15, targetGeneralOffers[0].Price);
        Assert.Single(targetTargetedOffers);
        Assert.Equal(25, targetTargetedOffers[0].Price);

        // Assert - non-target sees only general offer
        var nonTargetOffers = nonTarget.OffersSeenPerTick[2];
        var nonTargetGeneralOffers = nonTargetOffers.OfType<SellOffer>()
            .Where(o => o is not TargetedSellOffer).ToList();
        var nonTargetTargetedOffers = nonTargetOffers.OfType<TargetedSellOffer>().ToList();

        Assert.Single(nonTargetGeneralOffers);
        Assert.Equal(15, nonTargetGeneralOffers[0].Price);
        Assert.Empty(nonTargetTargetedOffers);
    }
}
