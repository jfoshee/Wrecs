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

    public AgentIntent GetIntent(IAgentContext context)
    {
        if (_hasMadeOffer)
            return AgentIntent.Empty;
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

    public AgentIntent GetIntent(IAgentContext context)
    {
        var opportunities = context.GetSnapshot<OfferListSnapshot>().Offers?.ToList() ?? [];
        OffersSeenPerTick.Add([.. opportunities]);
        var targetedOffer = opportunities.OfType<TargetedSellOffer>()
            .FirstOrDefault(o => o.Buyer == this);
        if (targetedOffer is not null)
            return new(new TakeOfferDecision(targetedOffer));
        return AgentIntent.Empty;
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

    public AgentIntent GetIntent(IAgentContext context)
    {
        var opportunities = context.GetSnapshot<OfferListSnapshot>().Offers?.ToList() ?? [];
        OffersSeenPerTick.Add([.. opportunities]);
        return AgentIntent.Empty;
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

    public AgentIntent GetIntent(IAgentContext context)
    {
        _tickCount++;
        return _tickCount switch
        {
            1 => new AgentIntent(new MakeOfferDecision(new SellOffer(this, generalPrice, generalResources))),
            2 => new AgentIntent(new MakeOfferDecision(new TargetedSellOffer(this, target, targetedPrice, targetedResources))),
            _ => AgentIntent.Empty
        };
    }
}

/// <summary>
/// Makes a targeted buy offer to a specific seller on the first tick.
/// </summary>
class MakesTargetedBuyOfferAgent(ICommercialAgent target, int price, int resources) : ICommercialAgent
{
    public int Id { get; } = EntityId.Next();
    public string Name => nameof(MakesTargetedBuyOfferAgent);
    private bool _hasMadeOffer = false;

    public AgentIntent GetIntent(IAgentContext context)
    {
        if (_hasMadeOffer)
            return AgentIntent.Empty;
        _hasMadeOffer = true;
        return new(new MakeOfferDecision(new TargetedBuyOffer(this, target, price, resources)));
    }
}

/// <summary>
/// Tracks all offers seen and takes the first targeted buy offer aimed at it.
/// </summary>
class TargetedBuyOfferReceiverAgent : ICommercialAgent
{
    public int Id { get; } = EntityId.Next();
    public string Name => nameof(TargetedBuyOfferReceiverAgent);

    public List<List<Offer>> OffersSeenPerTick { get; } = [];

    public AgentIntent GetIntent(IAgentContext context)
    {
        var opportunities = context.GetSnapshot<OfferListSnapshot>().Offers?.ToList() ?? [];
        OffersSeenPerTick.Add([.. opportunities]);
        var targetedOffer = opportunities.OfType<TargetedBuyOffer>()
            .FirstOrDefault(o => o.Seller == this);
        if (targetedOffer is not null)
            return new(new TakeOfferDecision(targetedOffer));
        return AgentIntent.Empty;
    }
}

/// <summary>
/// Makes a general buy offer on tick 1, then a targeted buy offer on tick 2.
/// </summary>
class MakesGeneralAndTargetedBuyOffersAgent(
    ICommercialAgent target,
    int generalPrice, int generalResources,
    int targetedPrice, int targetedResources) : ICommercialAgent
{
    public int Id { get; } = EntityId.Next();
    public string Name => nameof(MakesGeneralAndTargetedBuyOffersAgent);
    private int _tickCount = 0;

    public AgentIntent GetIntent(IAgentContext context)
    {
        _tickCount++;
        return _tickCount switch
        {
            1 => new AgentIntent(new MakeOfferDecision(new BuyOffer(this, generalPrice, generalResources))),
            2 => new AgentIntent(new MakeOfferDecision(new TargetedBuyOffer(this, target, targetedPrice, targetedResources))),
            _ => AgentIntent.Empty
        };
    }
}

/// <summary>
/// Makes a single offer (built from itself) on the first tick, then tracks all offers seen every tick.
/// Used to verify an agent can see offers it authored, even when targeted at someone else.
/// </summary>
class MakesOfferThenObservesAgent(Func<ICommercialAgent, Offer> offerFactory) : ICommercialAgent
{
    public int Id { get; } = EntityId.Next();
    public string Name => nameof(MakesOfferThenObservesAgent);
    private bool _hasMadeOffer = false;

    public List<List<Offer>> OffersSeenPerTick { get; } = [];

    public AgentIntent GetIntent(IAgentContext context)
    {
        var opportunities = context.GetSnapshot<OfferListSnapshot>().Offers?.ToList() ?? [];
        OffersSeenPerTick.Add([.. opportunities]);
        if (_hasMadeOffer)
            return AgentIntent.Empty;
        _hasMadeOffer = true;
        return new(new MakeOfferDecision(offerFactory(this)));
    }
}

#endregion

public class TargetedOfferTests
{
    [Fact(DisplayName = "Targeted Sell Offer: Target Sees and Takes the Offer")]
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

    [Fact(DisplayName = "Targeted Sell Offer: Non-Target Does Not See the Offer")]
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

    [Fact(DisplayName = "Targeted Sell Offers: Same Target Sees Offers From Multiple Sellers")]
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

    [Fact(DisplayName = "Targeted Sell Offers: Different Targets Each See Only Their Own Offer")]
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

    [Fact(DisplayName = "Mixed General and Targeted Sell Offers: Target Sees Both, Non-Target Sees Only General")]
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

    [Fact(DisplayName = "Targeted Buy Offer: Target Sees and Takes the Offer")]
    public void BasicTargetedBuyOffer_TargetSeesAndTakesOffer()
    {
        // Arrange
        var seller = new TargetedBuyOfferReceiverAgent();
        var buyer = new MakesTargetedBuyOfferAgent(seller, price: 10, resources: 5);
        var sim = new CommercialSimHarness();
        sim.InitEntities(
            (buyer, new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 0)),
            (seller, new CommercialSnapshot(MoneyBalance: 0, ResourceBalance: 50)));

        // Act - Tick 1: buyer makes targeted offer
        sim.Tick();
        // Act - Tick 2: seller sees and takes the offer
        sim.Tick();

        // Assert - seller should have sold to the buyer
        var buyerState = sim.GetCommercialState(buyer);
        var sellerState = sim.GetCommercialState(seller);

        Assert.Equal(90, buyerState.MoneyBalance);    // Paid 10 for resources
        Assert.Equal(5, buyerState.ResourceBalance);  // Received 5 resources
        Assert.Equal(10, sellerState.MoneyBalance);   // Received 10 for resources
        Assert.Equal(45, sellerState.ResourceBalance); // Sold 5 resources
    }

    [Fact(DisplayName = "Targeted Buy Offer: Non-Target Does Not See the Offer")]
    public void TargetedBuyOffer_NonTargetDoesNotSeeOffer()
    {
        // Arrange
        var target = new OfferObserverAgent { Name = "Target" };
        var nonTarget = new OfferObserverAgent { Name = "NonTarget" };
        var buyer = new MakesTargetedBuyOfferAgent(target, price: 10, resources: 5);
        var sim = new CommercialSimHarness();
        sim.InitEntities(
            (buyer, new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 0)),
            (target, new CommercialSnapshot(MoneyBalance: 0, ResourceBalance: 50)),
            (nonTarget, new CommercialSnapshot(MoneyBalance: 0, ResourceBalance: 50)));

        // Act - Tick 1: buyer makes targeted offer
        sim.Tick();
        // Act - Tick 2: agents observe offers
        sim.Tick();

        // Assert - target sees the offer, non-target does not
        var targetOffersOnTick2 = target.OffersSeenPerTick[1];
        var nonTargetOffersOnTick2 = nonTarget.OffersSeenPerTick[1];

        Assert.Single(targetOffersOnTick2.OfType<TargetedBuyOffer>());
        Assert.Empty(nonTargetOffersOnTick2.OfType<TargetedBuyOffer>());
    }

    [Fact(DisplayName = "Targeted Buy Offers: Same Target Sees Offers From Multiple Buyers")]
    public void MultipleTargetedBuyOffersToSameTarget_TargetSeesAll()
    {
        // Arrange
        var seller = new OfferObserverAgent { Name = "Seller" };
        var buyer1 = new MakesTargetedBuyOfferAgent(seller, price: 10, resources: 5);
        var buyer2 = new MakesTargetedBuyOfferAgent(seller, price: 20, resources: 3);
        var sim = new CommercialSimHarness();
        sim.InitEntities(
            (buyer1, new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 0)),
            (buyer2, new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 0)),
            (seller, new CommercialSnapshot(MoneyBalance: 0, ResourceBalance: 100)));

        // Act - Tick 1: both buyers make targeted offers
        sim.Tick();
        // Act - Tick 2: seller observes offers
        sim.Tick();

        // Assert - seller sees both targeted offers
        var sellerOffersOnTick2 = seller.OffersSeenPerTick[1];
        var targetedOffers = sellerOffersOnTick2.OfType<TargetedBuyOffer>().ToList();

        Assert.Equal(2, targetedOffers.Count);
        Assert.Contains(targetedOffers, o => o.Price == 10 && o.Resources == 5);
        Assert.Contains(targetedOffers, o => o.Price == 20 && o.Resources == 3);
    }

    [Fact(DisplayName = "Targeted Buy Offers: Different Targets Each See Only Their Own Offer")]
    public void DifferentTargets_EachSeesOnlyTheirOwnBuyOffers()
    {
        // Arrange
        var sellerA = new OfferObserverAgent { Name = "SellerA" };
        var sellerB = new OfferObserverAgent { Name = "SellerB" };
        var buyerA = new MakesTargetedBuyOfferAgent(sellerA, price: 100, resources: 1);
        var buyerB = new MakesTargetedBuyOfferAgent(sellerB, price: 200, resources: 2);
        var sim = new CommercialSimHarness();
        sim.InitEntities(
            (buyerA, new CommercialSnapshot(MoneyBalance: 500, ResourceBalance: 0)),
            (buyerB, new CommercialSnapshot(MoneyBalance: 500, ResourceBalance: 0)),
            (sellerA, new CommercialSnapshot(MoneyBalance: 0, ResourceBalance: 100)),
            (sellerB, new CommercialSnapshot(MoneyBalance: 0, ResourceBalance: 100)));

        // Act - Tick 1: buyers make targeted offers to their respective targets
        sim.Tick();
        // Act - Tick 2: sellers observe offers
        sim.Tick();

        // Assert - each seller sees only the offer targeted at them
        var sellerAOffers = sellerA.OffersSeenPerTick[1].OfType<TargetedBuyOffer>().ToList();
        var sellerBOffers = sellerB.OffersSeenPerTick[1].OfType<TargetedBuyOffer>().ToList();

        Assert.Single(sellerAOffers);
        Assert.Equal(100, sellerAOffers[0].Price);
        Assert.Equal(1, sellerAOffers[0].Resources);
        Assert.Equal(buyerA, sellerAOffers[0].Buyer);

        Assert.Single(sellerBOffers);
        Assert.Equal(200, sellerBOffers[0].Price);
        Assert.Equal(2, sellerBOffers[0].Resources);
        Assert.Equal(buyerB, sellerBOffers[0].Buyer);
    }

    [Fact(DisplayName = "Mixed General and Targeted Buy Offers: Target Sees Both, Non-Target Sees Only General")]
    public void MixedGeneralAndTargetedBuyOffers_TargetSeesBoth_NonTargetSeesOnlyGeneral()
    {
        // Arrange
        var target = new OfferObserverAgent { Name = "Target" };
        var nonTarget = new OfferObserverAgent { Name = "NonTarget" };
        // Agent that makes both a general and targeted buy offer
        var buyer = new MakesGeneralAndTargetedBuyOffersAgent(target,
            generalPrice: 15, generalResources: 2,
            targetedPrice: 25, targetedResources: 4);
        var sim = new CommercialSimHarness();
        sim.InitEntities(
            (buyer, new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 0)),
            (target, new CommercialSnapshot(MoneyBalance: 0, ResourceBalance: 100)),
            (nonTarget, new CommercialSnapshot(MoneyBalance: 0, ResourceBalance: 100)));

        // Act - Tick 1 & 2: buyer makes both offers (takes 2 ticks to make both)
        sim.Tick();
        sim.Tick();
        // Act - Tick 3: agents observe all offers
        sim.Tick();

        // Assert - target sees both general and targeted offers
        var targetOffers = target.OffersSeenPerTick[2];
        var targetGeneralOffers = targetOffers.OfType<BuyOffer>()
            .Where(o => o is not TargetedBuyOffer).ToList();
        var targetTargetedOffers = targetOffers.OfType<TargetedBuyOffer>().ToList();

        Assert.Single(targetGeneralOffers);
        Assert.Equal(15, targetGeneralOffers[0].Price);
        Assert.Single(targetTargetedOffers);
        Assert.Equal(25, targetTargetedOffers[0].Price);

        // Assert - non-target sees only the general offer
        var nonTargetOffers = nonTarget.OffersSeenPerTick[2];
        var nonTargetGeneralOffers = nonTargetOffers.OfType<BuyOffer>()
            .Where(o => o is not TargetedBuyOffer).ToList();
        var nonTargetTargetedOffers = nonTargetOffers.OfType<TargetedBuyOffer>().ToList();

        Assert.Single(nonTargetGeneralOffers);
        Assert.Equal(15, nonTargetGeneralOffers[0].Price);
        Assert.Empty(nonTargetTargetedOffers);
    }

    [Fact(DisplayName = "Targeted Sell Offer: Author Sees Its Own Offer")]
    public void AuthorSeesItsOwnTargetedSellOffer()
    {
        // Arrange
        var target = new OfferObserverAgent { Name = "Target" };
        var seller = new MakesOfferThenObservesAgent(self => new TargetedSellOffer(self, target, Price: 10, Resources: 5));
        var sim = new CommercialSimHarness();
        sim.InitEntities(
            (seller, new CommercialSnapshot(MoneyBalance: 0, ResourceBalance: 100)),
            (target, new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 0)));

        // Act - Tick 1: seller makes the targeted offer
        sim.Tick();
        // Act - Tick 2: seller's own snapshot should now include the offer it authored
        sim.Tick();

        // Assert
        var sellerOffersOnTick2 = seller.OffersSeenPerTick[1];
        Assert.Single(sellerOffersOnTick2.OfType<TargetedSellOffer>());
    }

    [Fact(DisplayName = "Targeted Buy Offer: Author Sees Its Own Offer")]
    public void AuthorSeesItsOwnTargetedBuyOffer()
    {
        // Arrange
        var target = new OfferObserverAgent { Name = "Target" };
        var buyer = new MakesOfferThenObservesAgent(self => new TargetedBuyOffer(self, target, Price: 10, Resources: 5));
        var sim = new CommercialSimHarness();
        sim.InitEntities(
            (buyer, new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 0)),
            (target, new CommercialSnapshot(MoneyBalance: 0, ResourceBalance: 100)));

        // Act - Tick 1: buyer makes the targeted offer
        sim.Tick();
        // Act - Tick 2: buyer's own snapshot should now include the offer it authored
        sim.Tick();

        // Assert
        var buyerOffersOnTick2 = buyer.OffersSeenPerTick[1];
        Assert.Single(buyerOffersOnTick2.OfType<TargetedBuyOffer>());
    }
}
