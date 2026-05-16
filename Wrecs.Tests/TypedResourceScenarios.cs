namespace Wrecs.Tests;

public class TypedResourceScenarios
{
    [Fact(DisplayName = "Typed resource trade: gold for money")]
    public void TypedResourceTrade_GoldForMoney()
    {
        var sim = new Sim();
        var buyer = new AlwaysBuyingTaker();
        var seller = MockAgent();
        IStateSnapshot[] initSellerState =
        [
            new CommercialSnapshot(100, [("gold", 50)]),
            new OfferListSnapshot([new(Seller: seller, Buyer: null, Price: 20, Resources: 10, ResourceType: "gold")])
        ];
        sim.InitEntities(
            (buyer, [new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 0)]),
            (seller, initSellerState));

        sim.Tick();

        var buyerState = sim.GetCommercialState(buyer);
        buyerState.MoneyBalance.Should().Be(80);
        buyerState.GetResourceBalance("gold").Should().Be(10);

        var sellerState = sim.GetCommercialState(seller);
        sellerState.MoneyBalance.Should().Be(120);
        sellerState.GetResourceBalance("gold").Should().Be(40);
    }

    [Fact(DisplayName = "Different resource types are independent")]
    public void DifferentResourceTypes_AreIndependent()
    {
        var sim = new CommercialSimHarness();
        var trader = MockAgent();
        sim.InitEntities((trader, new(0, [("gold", 100), ("silver", 200)])));

        var state = sim.GetCommercialState(trader);
        state.GetResourceBalance("gold").Should().Be(100);
        state.GetResourceBalance("silver").Should().Be(200);
        state.GetResourceBalance("bronze").Should().Be(0);
        state.ResourceBalance.Should().Be(0); // unitless is separate
    }

    [Fact(DisplayName = "Cannot sell typed resource you don't have")]
    public void CannotSellTypedResource_YouDontHave()
    {
        var sim = new Sim();
        var buyer = new AlwaysBuyingTaker();
        // Seller has gold but not silver; trying to sell silver
        var seller = MockAgent();
        IStateSnapshot[] initSellerState =
        [
            new CommercialSnapshot(0, [("gold", 50)]),
            new OfferListSnapshot([new(Seller: seller, Buyer: null, Price: 20, Resources: 10, ResourceType: "silver")])
        ];
        sim.InitEntities(
            (buyer, [new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 0)]),
            (seller, initSellerState));

        sim.Tick();

        // Trade should not execute - seller doesn't have silver
        var buyerState = sim.GetCommercialState(buyer);
        buyerState.MoneyBalance.Should().Be(100);
        buyerState.GetResourceBalance("silver").Should().Be(0);

        var sellerState = sim.GetCommercialState(seller);
        sellerState.MoneyBalance.Should().Be(0);
        sellerState.GetResourceBalance("gold").Should().Be(50);
    }

    [Fact(DisplayName = "Gold and unitless resources are separate")]
    public void GoldAndUnitless_AreSeparate()
    {
        var sim = new Sim();
        var buyer = new AlwaysBuyingTaker();
        // Seller has unitless resources but not gold; tries to sell gold
        var seller = MockAgent();
        IStateSnapshot[] initSellerState =
        [
            new CommercialSnapshot(0, ResourceBalance: 50),
            new OfferListSnapshot([new(Seller: seller, Buyer: null, Price: 20, Resources: 10, ResourceType: "gold")])
        ];
        sim.InitEntities(
            (buyer, [new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 0)]),
            (seller, initSellerState));

        sim.Tick();

        // Trade should not execute - seller doesn't have gold
        sim.GetCommercialState(buyer).MoneyBalance.Should().Be(100);
        sim.GetCommercialState(seller).MoneyBalance.Should().Be(0);
    }

    [Fact(DisplayName = "Source grants typed resources")]
    public void SourceGrants_TypedResources()
    {
        var sim = new CommercialSimHarness();
        var miner = new DoNothingAgent();
        var goldMine = new FixedGrantSource(miner, money: 0, resources: 10, resourceType: "gold");
        sim.InitEntities((miner, default));
        sim.InitControllers(new MoneySourcesController(goldMine), new ResourceSourcesController(goldMine));

        sim.Tick();
        sim.Tick();

        var state = sim.GetCommercialState(miner);
        state.GetResourceBalance("gold").Should().Be(20);
        state.ResourceBalance.Should().Be(0); // unitless unchanged
    }

    [Fact(DisplayName = "Multiple typed grants accumulate correctly")]
    public void MultipleTypedGrants_AccumulateCorrectly()
    {
        var sim = new CommercialSimHarness();
        var miner = new DoNothingAgent();
        var goldMine = new FixedGrantSource(miner, money: 0, resources: 10, resourceType: "gold");
        var silverMine = new FixedGrantSource(miner, money: 0, resources: 25, resourceType: "silver");
        sim.InitEntities((miner, default));
        sim.InitControllers(new MoneySourcesController(goldMine, silverMine), new ResourceSourcesController(goldMine, silverMine));

        sim.Tick();

        var state = sim.GetCommercialState(miner);
        state.GetResourceBalance("gold").Should().Be(10);
        state.GetResourceBalance("silver").Should().Be(25);
    }

    [Fact(DisplayName = "Buy offer for typed resource")]
    public void BuyOffer_ForTypedResource()
    {
        var sim = new Sim();
        var seller = new AlwaysSellingTaker();
        var buyer = MockAgent();
        IStateSnapshot[] initBuyerState =
        [
            new CommercialSnapshot(200, 0),
            new OfferListSnapshot([new(Seller: null, Buyer: buyer, Price: 50, Resources: 25, ResourceType: "gold")])
        ];
        sim.InitEntities(
            (seller, [new CommercialSnapshot(0, [("gold", 100)])]),
            (buyer, initBuyerState));

        sim.Tick();

        var sellerState = sim.GetCommercialState(seller);
        sellerState.MoneyBalance.Should().Be(50);
        sellerState.GetResourceBalance("gold").Should().Be(75);

        var buyerState = sim.GetCommercialState(buyer);
        buyerState.MoneyBalance.Should().Be(150);
        buyerState.GetResourceBalance("gold").Should().Be(25);
    }

    [Fact(DisplayName = "Agent makes typed sell offer, other takes it")]
    public void AgentMakesTypedSellOffer_OtherTakesIt()
    {
        var sim = new CommercialSimHarness();
        var seller = new MakesSellOfferAgent(price: 15, resources: 5, resourceType: "silver");
        var buyer = new AlwaysBuyingTaker();
        sim.InitEntities(
            (seller, new(0, [("silver", 100)])),
            (buyer, new(MoneyBalance: 100, ResourceBalance: 0)));

        sim.Tick(); // Offer made
        sim.Tick(); // Offer taken

        var sellerState = sim.GetCommercialState(seller);
        sellerState.MoneyBalance.Should().Be(15);
        sellerState.GetResourceBalance("silver").Should().Be(95);

        var buyerState = sim.GetCommercialState(buyer);
        buyerState.MoneyBalance.Should().Be(85);
        buyerState.GetResourceBalance("silver").Should().Be(5);
    }

    [Fact(DisplayName = "Mixed unitless and typed resources coexist")]
    public void MixedUnitlessAndTyped_Coexist()
    {
        var sim = new Sim();
        var buyer = new AlwaysBuyingTaker();
        var seller = MockAgent();
        IStateSnapshot[] initSellerState =
        [
            new CommercialSnapshot(0, [("", 50), ("gold", 50)]),
            new OfferListSnapshot([new(Seller: seller, Buyer: null, Price: 20, Resources: 10, ResourceType: "gold")])
        ];
        sim.InitEntities(
            (buyer, [new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 0)]),
            (seller, initSellerState));

        sim.Tick();

        // Gold trade executes
        var sellerState = sim.GetCommercialState(seller);
        sellerState.MoneyBalance.Should().Be(20);
        sellerState.ResourceBalance.Should().Be(50); // unitless unchanged
        sellerState.GetResourceBalance("gold").Should().Be(40);

        var buyerState = sim.GetCommercialState(buyer);
        buyerState.MoneyBalance.Should().Be(80);
        buyerState.GetResourceBalance("gold").Should().Be(10);
        buyerState.ResourceBalance.Should().Be(0); // unitless unchanged
    }

    private static ICommercialAgent MockAgent()
    {
        var id = EntityId.Next();
        return Mock.Of<ICommercialAgent>(a => a.Name == Guid.NewGuid().ToString() && a.Id == id);
    }
}
