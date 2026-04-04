namespace CommerceSim.Core.Tests;

public class BasicScenarios
{
    [Fact(DisplayName = "No Agents, No Offers")]
    public void NoAgentsNoOffers()
    {
        var sim = new Sim();
        sim.Tick();
    }

    [Fact(DisplayName = "One Agent, No Offers")]
    public void OneAgentNoOffers()
    {
        var sim = new Sim();
        var agent = MockAgent();
        sim.InitAgents((agent, default));
        sim.InitOffers();

        sim.Tick();

        var agentState = sim.GetState(agent);
        agentState.MoneyBalance.Should().Be(0);
        agentState.ResourceBalance.Should().Be(0);
    }

    [Fact(DisplayName = "Two Agents With State, No Offers")]
    public void TwoAgentsWithStateNoOffers()
    {
        var sim = new Sim();
        var agent1 = MockAgent();
        var agent2 = MockAgent();
        sim.InitAgents((agent1, new(MoneyBalance: 4, ResourceBalance: 8)),
                       (agent2, new(MoneyBalance: 3, ResourceBalance: 7)));
        sim.InitOffers();

        sim.Tick();

        var state1 = sim.GetState(agent1);
        state1.MoneyBalance.Should().Be(4);
        state1.ResourceBalance.Should().Be(8);
        var state2 = sim.GetState(agent2);
        state2.MoneyBalance.Should().Be(3);
        state2.ResourceBalance.Should().Be(7);
    }

    [Fact(DisplayName = "Two Agents, One Sell Offer, One Buyer")]
    public void TwoAgentsOneSellOfferOneBuyer()
    {
        var sim = new Sim();
        var buyer = new AlwaysBuyingTaker();
        var seller = MockAgent();
        sim.InitAgents((buyer, new(MoneyBalance: 32, ResourceBalance: 9)),
                       (seller, new(MoneyBalance: 64, ResourceBalance: 27)));
        sim.InitOffers(new SellOffer(Seller: seller, Price: 7, Resources: 5));

        sim.Tick();

        var buyerState = sim.GetState(buyer);
        buyerState.MoneyBalance.Should().Be(32 - 7);
        buyerState.ResourceBalance.Should().Be(9 + 5);
        var sellerState = sim.GetState(seller);
        sellerState.MoneyBalance.Should().Be(64 + 7);
        sellerState.ResourceBalance.Should().Be(27 - 5);
    }

    [Fact(DisplayName = "Two Agents, One Buy Offer, One Seller")]
    public void TwoAgentsOneBuyOfferOneSeller()
    {
        var sim = new Sim();
        var seller = new AlwaysSellingTaker();
        var buyer = MockAgent();
        sim.InitAgents((seller, new(MoneyBalance: 32, ResourceBalance: 9)),
                       (buyer, new(MoneyBalance: 64, ResourceBalance: 27)));
        sim.InitOffers(new BuyOffer(Buyer: buyer, Price: 7, Resources: 5));

        sim.Tick();

        var sellerState = sim.GetState(seller);
        sellerState.MoneyBalance.Should().Be(32 + 7);
        sellerState.ResourceBalance.Should().Be(9 - 5);
        var buyerState = sim.GetState(buyer);
        buyerState.MoneyBalance.Should().Be(64 - 7);
        buyerState.ResourceBalance.Should().Be(27 + 5);
    }

    [Fact(DisplayName = "Two Agents, Two Offers, No Takers")]
    public void TwoOffersNoTakers()
    {
        var sim = new Sim();
        var buyer = MockAgent();
        var seller = MockAgent();
        sim.InitAgents((buyer, new(MoneyBalance: 32, ResourceBalance: 9)),
                       (seller, new(MoneyBalance: 64, ResourceBalance: 27)));
        sim.InitOffers(new SellOffer(Seller: seller, Price: 7, Resources: 5),
                       new BuyOffer(Buyer: buyer, Price: 7, Resources: 5));

        sim.Tick();

        var buyerState = sim.GetState(buyer);
        buyerState.MoneyBalance.Should().Be(32);
        buyerState.ResourceBalance.Should().Be(9);
        var sellerState = sim.GetState(seller);
        sellerState.MoneyBalance.Should().Be(64);
        sellerState.ResourceBalance.Should().Be(27);

        // Another tick yields same result
        sim.Tick();

        // Verify unchanged (value based equality)
        sim.GetState(buyer).Should().Be(buyerState);
        sim.GetState(seller).Should().Be(sellerState);
    }

    [Fact(DisplayName = "Consumed offer has no effect on next tick")]
    public void ConsumedOfferHasNoEffectOnNextTick()
    {
        var sim = new Sim();
        var buyer = new AlwaysBuyingTaker();
        var seller = MockAgent();
        sim.InitAgents((buyer, new(MoneyBalance: 100, ResourceBalance: 0)),
                       (seller, new(MoneyBalance: 0, ResourceBalance: 50)));
        sim.InitOffers(new SellOffer(Seller: seller, Price: 10, Resources: 5));

        // First tick: buyer consumes the offer
        sim.Tick();

        var buyerStateAfterTick1 = sim.GetState(buyer);
        buyerStateAfterTick1.MoneyBalance.Should().Be(100 - 10);
        buyerStateAfterTick1.ResourceBalance.Should().Be(0 + 5);
        var sellerStateAfterTick1 = sim.GetState(seller);
        sellerStateAfterTick1.MoneyBalance.Should().Be(0 + 10);
        sellerStateAfterTick1.ResourceBalance.Should().Be(50 - 5);

        // Second tick: offer is gone, no change
        sim.Tick();

        sim.GetState(buyer).Should().Be(buyerStateAfterTick1);
        sim.GetState(seller).Should().Be(sellerStateAfterTick1);
    }

    [Fact(DisplayName = "Agent Makes Sell Offer, Other Agent Takes It")]
    public void AgentMakesSellOfferOtherAgentTakesIt()
    {
        var sim = new Sim();
        var seller = new MakesSellOfferAgent(price: 8, resources: 3);
        var buyer = new AlwaysBuyingTaker();
        AgentStateSnapshot initialSellerState = new(MoneyBalance: 0, ResourceBalance: 100);
        AgentStateSnapshot initialBuyerState = new(MoneyBalance: 64, ResourceBalance: 0);
        sim.InitAgents((seller, initialSellerState),
                       (buyer, initialBuyerState));

        sim.Tick();

        // State should be unchanged because the offer has been made but not taken yet
        sim.GetState(seller).Should().Be(initialSellerState);
        sim.GetState(buyer).Should().Be(initialBuyerState);

        sim.Tick();

        // Verify the offer was taken and state updated accordingly
        var sellerState = sim.GetState(seller);
        sellerState.Should()
            .Be(new AgentStateSnapshot(MoneyBalance: 8, ResourceBalance: 97));
        var buyerState = sim.GetState(buyer);
        buyerState.Should()
            .Be(new AgentStateSnapshot(MoneyBalance: 64 - 8, ResourceBalance: 3));

        sim.Tick();

        // Verify no further changes (offer was consumed)
        sim.GetState(seller).Should().Be(sellerState);
        sim.GetState(buyer).Should().Be(buyerState);
    }

    [Fact(DisplayName = "One Source, One Do-Nothing Agent")]
    public void OneSourceOneDoNothingAgent()
    {
        var sim = new Sim();
        var agent = new DoNothingAgent();
        var source = new FixedGrantSource(agent, money: 16, resources: 42);
        sim.InitAgents((agent, default));
        sim.InitSources(source);

        sim.Tick();

        var snapshot = sim.GetState(agent);
        snapshot.MoneyBalance.Should().Be(16);
        snapshot.ResourceBalance.Should().Be(42);
    }

    [Fact(DisplayName = "One Source, One Do-Nothing Agent, 2 Grants")]
    public void OneSourceOneDoNothingAgentTwoGrants()
    {
        var sim = new Sim();
        var agent = new DoNothingAgent();
        var source = new FixedGrantSource(agent, money: 10, resources: 300);
        sim.InitAgents((agent, default));
        sim.InitSources(source);

        sim.Tick();
        sim.Tick();

        var snapshot = sim.GetState(agent);
        snapshot.MoneyBalance.Should().Be(20);
        snapshot.ResourceBalance.Should().Be(600);
    }

    [Fact(DisplayName = "Two Sources, One Agent: Grants Add Up")]
    public void TwoSourcesOneAgentGrantsAddUp()
    {
        var sim = new Sim();
        var agent = new DoNothingAgent();
        var source1 = new FixedGrantSource(agent, money: 10, resources: 300);
        var source2 = new FixedGrantSource(agent, money: 5, resources: 20);
        sim.InitAgents((agent, default));
        sim.InitSources(source1, source2);

        sim.Tick();

        var snapshot = sim.GetState(agent);
        snapshot.MoneyBalance.Should().Be(15);
        snapshot.ResourceBalance.Should().Be(320);
    }

    [Fact(DisplayName = "Two Sources, Two Agents: Grants assigned to correct agent")]
    public void TwoSourcesTwoAgentsGrantsAssignedToCorrectAgent()
    {
        var sim = new Sim();
        var agent1 = MockAgent();
        var agent2 = MockAgent();
        var source1 = new FixedGrantSource(agent1, money: 3, resources: 5);
        var source2 = new FixedGrantSource(agent2, money: 7, resources: 11);
        sim.InitAgents((agent1, default),
                       (agent2, default));
        sim.InitSources(source1, source2);

        sim.Tick();

        // Verify each agent received the correct grant
        var snapshot1 = sim.GetState(agent1);
        snapshot1.MoneyBalance.Should().Be(3);
        snapshot1.ResourceBalance.Should().Be(5);
        var snapshot2 = sim.GetState(agent2);
        snapshot2.MoneyBalance.Should().Be(7);
        snapshot2.ResourceBalance.Should().Be(11);
    }

    private static IAgent MockAgent()
    {
        var id = Agents.AgentId.Next();
        return Mock.Of<IAgent>(a => a.Name == Guid.NewGuid().ToString() && a.Id == id);
    }
}
