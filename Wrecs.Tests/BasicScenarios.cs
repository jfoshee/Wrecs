namespace Wrecs.Tests;

public class BasicScenarios
{
    [Fact(DisplayName = "No Agents, No Offers")]
    public void NoAgentsNoOffers()
    {
        var sim = new CommercialSimHarness();
        sim.Tick();
    }

    [Fact(DisplayName = "One Agent, No Offers")]
    public void OneAgentNoOffers()
    {
        var sim = new CommercialSimHarness();
        var agent = MockAgent();
        sim.InitEntities((agent, default));

        sim.Tick();

        var agentState = sim.GetCommercialState(agent);
        agentState.MoneyBalance.Should().Be(0);
        agentState.ResourceBalance.Should().Be(0);
    }

    [Fact(DisplayName = "Two Agents With State, No Offers")]
    public void TwoAgentsWithStateNoOffers()
    {
        var sim = new CommercialSimHarness();
        var agent1 = MockAgent();
        var agent2 = MockAgent();
        sim.InitEntities((agent1, new(MoneyBalance: 4, ResourceBalance: 8)),
                       (agent2, new(MoneyBalance: 3, ResourceBalance: 7)));

        sim.Tick();

        var state1 = sim.GetCommercialState(agent1);
        state1.MoneyBalance.Should().Be(4);
        state1.ResourceBalance.Should().Be(8);
        var state2 = sim.GetCommercialState(agent2);
        state2.MoneyBalance.Should().Be(3);
        state2.ResourceBalance.Should().Be(7);
    }

    [Fact(DisplayName = "Two Agents, One Sell Offer, One Buyer")]
    public void TwoAgentsOneSellOfferOneBuyer()
    {
        var sim = new CommercialSim();
        var buyer = new AlwaysBuyingTaker();
        var seller = MockAgent();
        IStateSnapshot[] initSellerState =
        [
            new CommercialSnapshot(MoneyBalance: 64, ResourceBalance: 27),
            new OfferListSnapshot([new(seller, null, Price: 7, Resources: 5)])
        ];
        sim.InitEntities(
            (buyer, [new CommercialSnapshot(MoneyBalance: 32, ResourceBalance: 9)]),
            (seller, initSellerState)
        );

        sim.Tick();

        var buyerState = sim.GetCommercialState(buyer);
        buyerState.MoneyBalance.Should().Be(32 - 7);
        buyerState.ResourceBalance.Should().Be(9 + 5);
        var sellerState = sim.GetCommercialState(seller);
        sellerState.MoneyBalance.Should().Be(64 + 7);
        sellerState.ResourceBalance.Should().Be(27 - 5);
    }

    [Fact(DisplayName = "Two Agents, One Buy Offer, One Seller")]
    public void TwoAgentsOneBuyOfferOneSeller()
    {
        var sim = new CommercialSim();
        var seller = new AlwaysSellingTaker();
        var buyer = MockAgent();
        IStateSnapshot[] initBuyerState =
        [
            new CommercialSnapshot(MoneyBalance: 64, ResourceBalance: 27),
            new OfferListSnapshot([new(null, Buyer: buyer, Price: 7, Resources: 5)])
        ];
        sim.InitEntities((seller, [new CommercialSnapshot(MoneyBalance: 32, ResourceBalance: 9)]),
                       (buyer, initBuyerState));

        sim.Tick();

        var sellerState = sim.GetCommercialState(seller);
        sellerState.MoneyBalance.Should().Be(32 + 7);
        sellerState.ResourceBalance.Should().Be(9 - 5);
        var buyerState = sim.GetCommercialState(buyer);
        buyerState.MoneyBalance.Should().Be(64 - 7);
        buyerState.ResourceBalance.Should().Be(27 + 5);
    }

    [Fact(DisplayName = "Two Agents, Two Offers, No Takers")]
    public void TwoOffersNoTakers()
    {
        var sim = new CommercialSim();
        var buyer = MockAgent();
        IStateSnapshot[] initBuyerState =
        [
            new CommercialSnapshot(MoneyBalance: 32, ResourceBalance: 9),
            new OfferListSnapshot([new(Seller: null, Buyer: buyer, Price: 7, Resources: 5)])
        ];
        var seller = MockAgent();
        IStateSnapshot[] initSellerState =
        [
            new CommercialSnapshot(MoneyBalance: 64, ResourceBalance: 27),
            new OfferListSnapshot([new(Seller: seller, null, Price: 7, Resources: 5)])
        ];
        sim.InitEntities(
        [
            (buyer, initBuyerState),
            (seller, initSellerState)
        ]);

        sim.Tick();

        var buyerState = sim.GetCommercialState(buyer);
        buyerState.MoneyBalance.Should().Be(32);
        buyerState.ResourceBalance.Should().Be(9);
        var sellerState = sim.GetCommercialState(seller);
        sellerState.MoneyBalance.Should().Be(64);
        sellerState.ResourceBalance.Should().Be(27);

        // Another tick yields same result
        sim.Tick();

        // Verify unchanged (value based equality)
        sim.GetCommercialState(buyer).Should().Be(buyerState);
        sim.GetCommercialState(seller).Should().Be(sellerState);
    }

    [Fact(DisplayName = "Consumed offer has no effect on next tick")]
    public void ConsumedOfferHasNoEffectOnNextTick()
    {
        var sim = new CommercialSim();
        var buyer = new AlwaysBuyingTaker();
        var seller = MockAgent();
        IStateSnapshot[] initSellerState = [
            new CommercialSnapshot(MoneyBalance: 0, ResourceBalance: 50),
            new OfferListSnapshot([new(Seller: seller, Buyer: null, Price: 10, Resources: 5)])
        ];
        sim.InitEntities((buyer, [new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 0)]),
                         (seller, initSellerState));

        // First tick: buyer consumes the offer
        sim.Tick();

        var buyerStateAfterTick1 = sim.GetCommercialState(buyer);
        buyerStateAfterTick1.MoneyBalance.Should().Be(100 - 10);
        buyerStateAfterTick1.ResourceBalance.Should().Be(0 + 5);
        var sellerStateAfterTick1 = sim.GetCommercialState(seller);
        sellerStateAfterTick1.MoneyBalance.Should().Be(0 + 10);
        sellerStateAfterTick1.ResourceBalance.Should().Be(50 - 5);

        // Second tick: offer is gone, no change
        sim.Tick();

        sim.GetCommercialState(buyer).Should().Be(buyerStateAfterTick1);
        sim.GetCommercialState(seller).Should().Be(sellerStateAfterTick1);
    }

    [Fact(DisplayName = "Agent Makes Sell Offer, Other Agent Takes It")]
    public void AgentMakesSellOfferOtherAgentTakesIt()
    {
        var sim = new CommercialSimHarness();
        var seller = new MakesSellOfferAgent(price: 8, resources: 3);
        var buyer = new AlwaysBuyingTaker();
        CommercialSnapshot initialSellerState = new(MoneyBalance: 0, ResourceBalance: 100);
        CommercialSnapshot initialBuyerState = new(MoneyBalance: 64, ResourceBalance: 0);
        sim.InitEntities((seller, initialSellerState),
                       (buyer, initialBuyerState));

        sim.Tick();

        // State should be unchanged because the offer has been made but not taken yet
        sim.GetCommercialState(seller).Should().Be(initialSellerState);
        sim.GetCommercialState(buyer).Should().Be(initialBuyerState);

        sim.Tick();

        // Verify the offer was taken and state updated accordingly
        var sellerState = sim.GetCommercialState(seller);
        sellerState.Should()
            .Be(new CommercialSnapshot(MoneyBalance: 8, ResourceBalance: 97));
        var buyerState = sim.GetCommercialState(buyer);
        buyerState.Should()
            .Be(new CommercialSnapshot(MoneyBalance: 64 - 8, ResourceBalance: 3));

        sim.Tick();

        // Verify no further changes (offer was consumed)
        sim.GetCommercialState(seller).Should().Be(sellerState);
        sim.GetCommercialState(buyer).Should().Be(buyerState);
    }

    [Fact(DisplayName = "Controller adds interest to all agents each tick")]
    public void ControllerAddsInterestToAllAgentsEachTick()
    {
        var sim = new CommercialSimHarness();
        var agent1 = MockAgent();
        var agent2 = MockAgent();
        var interestController = new InterestController(interestRate: 0.10);
        sim.InitEntities((agent1, new(MoneyBalance: 100, ResourceBalance: 0)),
                         (agent2, new(MoneyBalance: 200, ResourceBalance: 0)));
        sim.AddSystem(interestController);

        sim.Tick();

        // 10% interest added to each agent
        sim.GetCommercialState(agent1).MoneyBalance.Should().Be(110);
        sim.GetCommercialState(agent2).MoneyBalance.Should().Be(220);

        sim.Tick();

        // Compound interest: 10% of new balance
        sim.GetCommercialState(agent1).MoneyBalance.Should().Be(121);
        sim.GetCommercialState(agent2).MoneyBalance.Should().Be(242);
    }

    [Fact(DisplayName = "Controller grants resources to specific entity")]
    public void ControllerGrantsResourcesToSpecificEntity()
    {
        var sim = new CommercialSimHarness();
        var miner = new DoNothingAgent();
        var trader = MockAgent();
        var miningController = new MiningController(miner, resourcesPerTick: 5);
        sim.InitEntities((miner, new(MoneyBalance: 0, ResourceBalance: 10)),
                         (trader, new(MoneyBalance: 50, ResourceBalance: 0)));
        sim.AddSystem(miningController);

        sim.Tick();

        // Only the miner should receive resources
        sim.GetCommercialState(miner).ResourceBalance.Should().Be(15);
        sim.GetCommercialState(trader).ResourceBalance.Should().Be(0);

        sim.Tick();

        sim.GetCommercialState(miner).ResourceBalance.Should().Be(20);
        sim.GetCommercialState(trader).ResourceBalance.Should().Be(0);
    }

    private static ICommercialAgent MockAgent()
    {
        var id = EntityId.Next();
        return Mock.Of<ICommercialAgent>(a => a.Name == Guid.NewGuid().ToString() && a.Id == id);
    }
}
