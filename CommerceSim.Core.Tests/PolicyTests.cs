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
}