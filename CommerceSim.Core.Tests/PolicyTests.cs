namespace CommerceSim.Core.Tests;

public class PolicyTests
{
    [Fact(DisplayName = "Two buyers cannot consume the same offer")]
    public void TwoBuyersCannotConsumeTheSameOffer()
    {
        var sim = new Sim();
        var buyer1 = new AlwaysBuyingAgent();
        var buyer2 = new AlwaysBuyingAgent();
        var seller = new MakesSellOfferAgent(price: 8, resources: 3);
        AgentStateSnapshot sellerState0 = new(MoneyBalance: 0, ResourceBalance: 100);
        AgentStateSnapshot buyer1State0 = new(MoneyBalance: 32, ResourceBalance: 0);
        AgentStateSnapshot buyer2State0 = new(MoneyBalance: 64, ResourceBalance: 0);
        sim.InitAgents((seller, sellerState0),
                       (buyer1, buyer1State0),
                       (buyer2, buyer2State0));

        sim.Tick();

        // State should be unchanged because the offer has been made but not taken yet
        sim.GetState(seller).Should().Be(sellerState0);
        sim.GetState(buyer1).Should().Be(buyer1State0);
        sim.GetState(buyer2).Should().Be(buyer2State0);

        sim.Tick();

        // Verify the offer was taken by only one buyer and state updated accordingly
        var sellerState = sim.GetState(seller);
        sellerState.Should()
            .Be(new AgentStateSnapshot(MoneyBalance: 8, ResourceBalance: 97));
        var buyer1State = sim.GetState(buyer1);
        var buyer2State = sim.GetState(buyer2);
        ((buyer1State.MoneyBalance == 32 - 8 && buyer1State.ResourceBalance == 3) ||
         (buyer2State.MoneyBalance == 64 - 8 && buyer2State.ResourceBalance == 3))
            .Should().BeTrue();


        sim.Tick();

        // Verify no further changes (offer was consumed)
        sim.GetState(seller).Should().Be(sellerState);
        sim.GetState(buyer1).Should().Be(buyer1State);
        sim.GetState(buyer2).Should().Be(buyer2State);
    }

    [Fact(DisplayName = "Agent cannot sell more resources than it has")]
    public void AgentCannotSellMoreResourcesThanItHas()
    {
        // Setup a seller that wants to sell 20 resources, but only has 19
        var sim = new Sim();
        var seller = new MakesSellOfferAgent(price: 10, resources: 20);
        var buyer = new AlwaysBuyingAgent();
        AgentStateSnapshot sellerState0 = new(MoneyBalance: 0, ResourceBalance: 19);
        AgentStateSnapshot buyerState0 = new(MoneyBalance: 100, ResourceBalance: 0);
        sim.InitAgents((seller, sellerState0),
                       (buyer, buyerState0));

        sim.Tick(); // Seller makes offer to sell 20 resources
        sim.Tick(); // Buyer attempts to take the offer

        // Trade should be rejected because seller only has 19 resources
        sim.GetState(seller).Should().Be(sellerState0);
        sim.GetState(buyer).Should().Be(buyerState0);
    }

    [Fact(DisplayName = "Agent cannot buy more resources than seller has")]
    public void AgentCannotBuyMoreResourcesThanSellerHas()
    {
        // Setup a buyer that wants to buy 6 resources from a seller that only has 5
        var sim = new Sim();
        var buyer = new MakesBuyOfferAgent(price: 50, resources: 6);
        var seller = new AlwaysSellingAgent();
        AgentStateSnapshot buyerState0 = new(MoneyBalance: 100, ResourceBalance: 0);
        AgentStateSnapshot sellerState0 = new(MoneyBalance: 0, ResourceBalance: 5);
        sim.InitAgents((buyer, buyerState0),
                       (seller, sellerState0));

        sim.Tick(); // Buyer makes offer to buy 6 resources
        sim.Tick(); // Seller attempts to take the offer

        // Trade should be rejected because seller only has 5 resources
        sim.GetState(buyer).Should().Be(buyerState0);
        sim.GetState(seller).Should().Be(sellerState0);
    }

    [Fact(DisplayName = "Buyer taker cannot spend more money than it has")]
    public void BuyerCannotSpendMoreMoneyThanItHas()
    {
        // Setup a buyer that wants to buy resources for 50, but only has 49
        var sim = new Sim();
        var seller = new MakesSellOfferAgent(price: 50, resources: 5);
        var buyer = new AlwaysBuyingAgent();
        AgentStateSnapshot sellerState0 = new(MoneyBalance: 0, ResourceBalance: 100);
        AgentStateSnapshot buyerState0 = new(MoneyBalance: 49, ResourceBalance: 0);
        sim.InitAgents((seller, sellerState0),
                       (buyer, buyerState0));

        sim.Tick(); // Seller makes offer to sell for 50
        sim.Tick(); // Buyer attempts to take the offer

        // Trade should be rejected because buyer only has 49 money
        sim.GetState(seller).Should().Be(sellerState0);
        sim.GetState(buyer).Should().Be(buyerState0);
    }

    [Fact(DisplayName = "Buyer maker cannot spend more money than it has")]
    public void BuyerMakerCannotSpendMoreMoneyThanItHas()
    {
        // Setup a buyer that makes an offer to buy resources for 50, but only has 49
        var sim = new Sim();
        var buyer = new MakesBuyOfferAgent(price: 50, resources: 5);
        var seller = new AlwaysSellingAgent();
        AgentStateSnapshot buyerState0 = new(MoneyBalance: 49, ResourceBalance: 0);
        AgentStateSnapshot sellerState0 = new(MoneyBalance: 0, ResourceBalance: 100);
        sim.InitAgents((buyer, buyerState0),
                       (seller, sellerState0));

        sim.Tick(); // Buyer makes offer to buy for 50
        sim.Tick(); // Seller attempts to take the offer

        // Trade should be rejected because buyer only has 49 money
        sim.GetState(buyer).Should().Be(buyerState0);
        sim.GetState(seller).Should().Be(sellerState0);
    }
}
