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
        var loggingSim = new LoggingSim(sim);

        for (int i = 0; i < 100; i++)
        {
            loggingSim.Tick();
        }

        var valInvestorState = sim.GetState(valueInvestor);
        valInvestorState.MoneyBalance.Should().BeGreaterThan(100);

        // Serialize the snapshot log to CSV
        var snapshots = loggingSim.GetSnapshots();
        var agentNames = snapshots[0].Keys.OrderBy(name => name).ToList();
        using var writer = new StreamWriter("simulation_log.csv");
        writer.WriteLine("Tick," + string.Join(",", agentNames.Select(name => $"{name} Money,{name} Resources")));
        for (int tick = 0; tick < snapshots.Count; tick++)
        {
            var snapshot = snapshots[tick];
            var row = new List<string> { tick.ToString() };
            foreach (var agentName in agentNames)
            {
                var state = snapshot[agentName];
                row.Add(state.MoneyBalance.ToString());
                row.Add(state.ResourceBalance.ToString());
            }
            writer.WriteLine(string.Join(",", row));
        }
    }
}
