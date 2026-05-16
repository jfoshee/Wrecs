namespace Wrecs.Core.Tests;

public class PolicyTests
{
    [Fact(DisplayName = "Two buyers cannot consume the same offer")]
    public void TwoBuyersCannotConsumeTheSameOffer()
    {
        var sim = new CommercialSimHarness();
        var buyer1 = new AlwaysBuyingTaker();
        var buyer2 = new AlwaysBuyingTaker();
        var seller = new MakesSellOfferAgent(price: 8, resources: 3);
        CommercialSnapshot sellerState0 = new(MoneyBalance: 0, ResourceBalance: 100);
        CommercialSnapshot buyer1State0 = new(MoneyBalance: 32, ResourceBalance: 0);
        CommercialSnapshot buyer2State0 = new(MoneyBalance: 64, ResourceBalance: 0);
        sim.InitEntities((seller, sellerState0),
                       (buyer1, buyer1State0),
                       (buyer2, buyer2State0));

        sim.Tick();

        // State should be unchanged because the offer has been made but not taken yet
        sim.GetCommercialState(seller).Should().Be(sellerState0);
        sim.GetCommercialState(buyer1).Should().Be(buyer1State0);
        sim.GetCommercialState(buyer2).Should().Be(buyer2State0);

        sim.Tick();

        // Verify the offer was taken by only one buyer and state updated accordingly
        var sellerState = sim.GetCommercialState(seller);
        sellerState.Should()
            .Be(new CommercialSnapshot(MoneyBalance: 8, ResourceBalance: 97));
        var buyer1State = sim.GetCommercialState(buyer1);
        var buyer2State = sim.GetCommercialState(buyer2);
        ((buyer1State.MoneyBalance == 32 - 8 && buyer1State.ResourceBalance == 3) ||
         (buyer2State.MoneyBalance == 64 - 8 && buyer2State.ResourceBalance == 3))
            .Should().BeTrue();


        sim.Tick();

        // Verify no further changes (offer was consumed)
        sim.GetCommercialState(seller).Should().Be(sellerState);
        sim.GetCommercialState(buyer1).Should().Be(buyer1State);
        sim.GetCommercialState(buyer2).Should().Be(buyer2State);
    }

    [Fact(DisplayName = "Agent cannot sell more resources than it has")]
    public void AgentCannotSellMoreResourcesThanItHas()
    {
        // Setup a seller that wants to sell 20 resources, but only has 19
        var sim = new CommercialSimHarness();
        var seller = new MakesSellOfferAgent(price: 10, resources: 20);
        var buyer = new AlwaysBuyingTaker();
        CommercialSnapshot sellerState0 = new(MoneyBalance: 0, ResourceBalance: 19);
        CommercialSnapshot buyerState0 = new(MoneyBalance: 100, ResourceBalance: 0);
        sim.InitEntities((seller, sellerState0),
                       (buyer, buyerState0));

        sim.Tick(); // Seller makes offer to sell 20 resources
        sim.Tick(); // Buyer attempts to take the offer

        // Trade should be rejected because seller only has 19 resources
        sim.GetCommercialState(seller).Should().Be(sellerState0);
        sim.GetCommercialState(buyer).Should().Be(buyerState0);
    }

    [Fact(DisplayName = "Agent cannot buy more resources than seller has")]
    public void AgentCannotBuyMoreResourcesThanSellerHas()
    {
        // Setup a buyer that wants to buy 6 resources from a seller that only has 5
        var sim = new CommercialSimHarness();
        var buyer = new MakesBuyOfferAgent(price: 50, resources: 6);
        var seller = new AlwaysSellingTaker();
        CommercialSnapshot buyerState0 = new(MoneyBalance: 100, ResourceBalance: 0);
        CommercialSnapshot sellerState0 = new(MoneyBalance: 0, ResourceBalance: 5);
        sim.InitEntities((buyer, buyerState0),
                       (seller, sellerState0));

        sim.Tick(); // Buyer makes offer to buy 6 resources
        sim.Tick(); // Seller attempts to take the offer

        // Trade should be rejected because seller only has 5 resources
        sim.GetCommercialState(buyer).Should().Be(buyerState0);
        sim.GetCommercialState(seller).Should().Be(sellerState0);
    }

    [Fact(DisplayName = "Buyer taker cannot spend more money than it has")]
    public void BuyerCannotSpendMoreMoneyThanItHas()
    {
        // Setup a buyer that wants to buy resources for 50, but only has 49
        var sim = new CommercialSimHarness();
        var seller = new MakesSellOfferAgent(price: 50, resources: 5);
        var buyer = new AlwaysBuyingTaker();
        CommercialSnapshot sellerState0 = new(MoneyBalance: 0, ResourceBalance: 100);
        CommercialSnapshot buyerState0 = new(MoneyBalance: 49, ResourceBalance: 0);
        sim.InitEntities((seller, sellerState0),
                       (buyer, buyerState0));

        sim.Tick(); // Seller makes offer to sell for 50
        sim.Tick(); // Buyer attempts to take the offer

        // Trade should be rejected because buyer only has 49 money
        sim.GetCommercialState(seller).Should().Be(sellerState0);
        sim.GetCommercialState(buyer).Should().Be(buyerState0);
    }

    [Fact(DisplayName = "Buyer maker cannot spend more money than it has")]
    public void BuyerMakerCannotSpendMoreMoneyThanItHas()
    {
        // Setup a buyer that makes an offer to buy resources for 50, but only has 49
        var sim = new CommercialSimHarness();
        var buyer = new MakesBuyOfferAgent(price: 50, resources: 5);
        var seller = new AlwaysSellingTaker();
        CommercialSnapshot buyerState0 = new(MoneyBalance: 49, ResourceBalance: 0);
        CommercialSnapshot sellerState0 = new(MoneyBalance: 0, ResourceBalance: 100);
        sim.InitEntities((buyer, buyerState0),
                       (seller, sellerState0));

        sim.Tick(); // Buyer makes offer to buy for 50
        sim.Tick(); // Seller attempts to take the offer

        // Trade should be rejected because buyer only has 49 money
        sim.GetCommercialState(buyer).Should().Be(buyerState0);
        sim.GetCommercialState(seller).Should().Be(sellerState0);
    }

    [Fact(DisplayName = "Two buyers should have roughly equal outcomes")]
    public void TwoBuyersShouldHaveRoughlyEqualOutcomes()
    {
        // Setup: one seller continuously making offers, two competing buyers
        var sim = new CommercialSimHarness();
        var seller = new AlwaysSellingMaker(price: 10, resources: 1);
        var buyer1 = new AlwaysBuyingTaker();
        var buyer2 = new AlwaysBuyingTaker();
        CommercialSnapshot sellerState0 = new(MoneyBalance: 0, ResourceBalance: 1000);
        CommercialSnapshot buyerState0 = new(MoneyBalance: 1000, ResourceBalance: 0);
        sim.InitEntities((seller, sellerState0),
                       (buyer1, buyerState0),
                       (buyer2, buyerState0));

        for (int i = 0; i < 200; i++)
        {
            sim.Tick();
        }

        // Both buyers should have roughly the same ending balance and resources
        var buyer1State = sim.GetCommercialState(buyer1);
        var buyer2State = sim.GetCommercialState(buyer2);
        buyer1State.ResourceBalance.Should().BeCloseTo(buyer2State.ResourceBalance, delta: 10);
    }

    [Fact(DisplayName = "Source cannot take away resources")]
    public void SourceCannotTakeAwayResources()
    {
        var sim = new CommercialSimHarness();
        var agent = new DoNothingAgent();
        var source = new FixedGrantSource(agent, money: 0, resources: -10);
        CommercialSnapshot agentState0 = new(MoneyBalance: 0, ResourceBalance: 100);
        sim.InitEntities((agent, agentState0));
        sim.InitControllers(new MoneySourcesController(source), new ResourceSourcesController(source));

        sim.Tick();

        // State should be unchanged because the grant cannot take away resources
        sim.GetCommercialState(agent).Should().Be(agentState0);
    }

    [Fact(DisplayName = "Source cannot take away money")]
    public void SourceCannotTakeAwayMoney()
    {
        var sim = new CommercialSimHarness();
        var agent = new DoNothingAgent();
        var source = new FixedGrantSource(agent, money: -10, resources: 0);
        CommercialSnapshot agentState0 = new(MoneyBalance: 100, ResourceBalance: 0);
        sim.InitEntities((agent, agentState0));
        sim.InitControllers(new MoneySourcesController(source), new ResourceSourcesController(source));

        sim.Tick();

        // State should be unchanged because the grant cannot take away money
        sim.GetCommercialState(agent).Should().Be(agentState0);
    }

    class FixedSink(ICommercialAgent recipient, int money, int resources) : IMoneySink, IResourceSink
    {
        IEnumerable<MoneyFlow> IMoneyFlowOrigin.CreateFlows(FlowContext _)
        {
            if (money != 0) yield return MoneyFlow.Debit(recipient, money);
        }

        IEnumerable<ResourceFlow> IResourceFlowOrigin.CreateFlows(FlowContext _)
        {
            if (resources != 0) yield return ResourceFlow.Debit(recipient, resources);
        }
    }

    [Fact(DisplayName = "Sink cannot add money or resources (negative charge)")]
    public void SinkCannotAddMoneyOrResources()
    {
        var sim = new CommercialSimHarness();
        var agent = new DoNothingAgent();
        var sink1 = new FixedSink(agent, money: -10, resources: 0);
        var sink2 = new FixedSink(agent, money: 0, resources: -10);
        CommercialSnapshot agentState0 = new(MoneyBalance: 100, ResourceBalance: 0);
        sim.InitEntities((agent, agentState0));
        sim.InitControllers(new MoneySinksController(sink1, sink2), new ResourceSinksController(sink1, sink2));

        sim.Tick();

        // State should be unchanged because the sink cannot add money or resources
        sim.GetCommercialState(agent).Should().Be(agentState0);
    }

    [Fact(DisplayName = "Sink cannot force negative on both balances")]
    public void SinkCannotForceNegativeOnBothBalances()
    {
        var sim = new CommercialSimHarness();
        var agent = new DoNothingAgent();
        var sink = new FixedSink(agent, money: 150, resources: 150);
        CommercialSnapshot agentState0 = new(MoneyBalance: 100, ResourceBalance: 100);
        sim.InitEntities((agent, agentState0));
        sim.InitControllers(new MoneySinksController(sink), new ResourceSinksController(sink));

        sim.Tick();

        // State should be unchanged because sink cannot force negative balances
        sim.GetCommercialState(agent).Should().Be(agentState0);
    }

    [Fact(DisplayName = "Sink can take all money")]
    public void SinkCanTakeAllMoney()
    {
        var sim = new CommercialSimHarness();
        var agent = new DoNothingAgent();
        var sink = new FixedSink(agent, money: 100, resources: 0);
        CommercialSnapshot agentState0 = new(MoneyBalance: 100, ResourceBalance: 42);
        sim.InitEntities((agent, agentState0));
        sim.InitControllers(new MoneySinksController(sink), new ResourceSinksController(sink));

        sim.Tick();

        // Money balance should be zero but resource balance should be unchanged
        sim.GetCommercialState(agent).Should().Be(new CommercialSnapshot(MoneyBalance: 0, ResourceBalance: 42));
    }

    [Fact(DisplayName = "Sink can take all resources")]
    public void SinkCanTakeAllResources()
    {
        var sim = new CommercialSimHarness();
        var agent = new DoNothingAgent();
        var sink = new FixedSink(agent, money: 0, resources: 100);
        CommercialSnapshot agentState0 = new(MoneyBalance: 42, ResourceBalance: 100);
        sim.InitEntities((agent, agentState0));
        sim.InitControllers(new MoneySinksController(sink), new ResourceSinksController(sink));

        sim.Tick();

        // Resource balance should be zero but money balance should be unchanged
        sim.GetCommercialState(agent).Should().Be(new CommercialSnapshot(MoneyBalance: 42, ResourceBalance: 0));
    }
}
