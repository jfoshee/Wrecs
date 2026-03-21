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
        var agent = Mock.Of<Agent>();
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
        var agent1 = Mock.Of<Agent>();
        var agent2 = Mock.Of<Agent>();
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

    class AlwaysBuyingAgent : Agent
    {
        public override Decision Decide(AgentStateSnapshot _, List<Offer> opportunities)
        {
            // Take first sell offer
            var sellOffer = opportunities.OfType<SellOffer>().First();
            return new TakeOfferDecision(sellOffer);
        }
    }

    [Fact(DisplayName = "Two Agents, One Sell Offer, One Buyer")]
    public void TwoAgentsOneSellOfferOneBuyer()
    {
        var sim = new Sim();
        var buyer = new AlwaysBuyingAgent();
        var seller = Mock.Of<Agent>();
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

    class AlwaysSellingAgent : Agent
    {
        public override Decision Decide(AgentStateSnapshot _, List<Offer> opportunities)
        {
            // Take first buy offer
            var buyOffer = opportunities.OfType<BuyOffer>().First();
            return new TakeOfferDecision(buyOffer);
        }
    }

    [Fact(DisplayName = "Two Agents, One Buy Offer, One Seller")]
    public void TwoAgentsOneBuyOfferOneSeller()
    {
        var sim = new Sim();
        var seller = new AlwaysSellingAgent();
        var buyer = Mock.Of<Agent>();
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
        var buyer = Mock.Of<Agent>();
        var seller = Mock.Of<Agent>();
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
}
