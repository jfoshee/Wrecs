namespace CommerceSim.Core.Tests;

public class AdvancedScenarios
{
    [Fact(DisplayName = "One Rich Dumb Agent and One Value Investor Agent")]
    public void OneRichDumbAgentAndOneValueInvestorAgent()
    {
        var sim = new Sim();
        var sellTaker = new AlwaysSellingAgent();
        var buyTaker = new AlwaysBuyingAgent();
        var valueInvestor = new ValueInvestorAgent(initialFairPrice: 10,
                                                   maxPosition: 5,
                                                   minCashReserve: 10);
        sim.InitAgents((sellTaker, new(MoneyBalance: 1_000_000, ResourceBalance: 1_000_000)),
                       (buyTaker, new(MoneyBalance: 1_000_000, ResourceBalance: 0)),
                       (valueInvestor, new(MoneyBalance: 50, ResourceBalance: 0)));
        sim.InitOffers(new SellOffer(Seller: sellTaker, Price: 10, Resources: 5));

        for (int i = 0; i < 100; i++)
        {
            sim.Tick();
        }

        var valInvestorState = sim.GetState(valueInvestor);
        valInvestorState.MoneyBalance.Should().BeGreaterThan(100);
    }
}
