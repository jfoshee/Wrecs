using CommerceSim.Core.Agents;
using ScottPlot;

namespace CommerceSim.Core.Tests;

public class AdvancedScenarios
{
    [Fact(DisplayName = "One Rich Dumb Agent and One Value Investor Agent")]
    public void OneRichDumbAgentAndOneValueInvestorAgent()
    {
        var sim = new Sim();
        var sellTaker = new AlwaysSellingTaker();
        var buyTaker = new AlwaysBuyingTaker();
        var sellMaker = new AlwaysSellingMaker(10, 1);
        var randomAgent = new RandomAgent(maxPrice: 10);
        var valueInvestor = new ValueInvestorAgent(initialFairPrice: 10,
                                                   maxPosition: 5,
                                                   minCashReserve: 10);
        sim.InitAgents((sellTaker, new(MoneyBalance: 1_000, ResourceBalance: 1_000)),
                       (buyTaker, new(MoneyBalance: 1_000, ResourceBalance: 0)),
                       (valueInvestor, new(MoneyBalance: 50, ResourceBalance: 0)),
                       (randomAgent, new(MoneyBalance: 1_000, ResourceBalance: 100)),
                       (sellMaker, new(MoneyBalance: 1_000, ResourceBalance: 100)));
        sim.InitOffers(new SellOffer(Seller: sellTaker, Price: 10, Resources: 5));
        var loggingSim = new LoggingSim(sim);

        for (int i = 0; i < 250; i++)
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

    [Fact(DisplayName = "All Agent Types Plus 20 Random Agents")]
    public void AllAgentTypesPlusTwentyRandomAgents()
    {
        const int StartingMoney = 500;
        const int StartingResources = 50;

        var sim = new Sim();

        // One of each agent type from the Agents namespace
        var contrarianAgent = new ContrarianMeanReversionAgent();
        var marketMakerAgent = new InventoryAwareMarketMakerAgent(anchorPrice: 10, targetInventory: 25, inventoryTolerance: 10);
        var momentumAgent = new MomentumChaserAgent();
        var spreadSniperAgent = new SpreadSniperAgent(minProfitPerUnit: 2, maxInventory: 20, minCashReserve: 50);
        var randomAgent = new RandomAgent(maxPrice: 20);
        var valueInvestorAgent = new ValueInvestorAgent(initialFairPrice: 10, maxPosition: 20, minCashReserve: 50);

        var agentInitList = new List<(IAgent, AgentStateSnapshot)>
        {
            (contrarianAgent, new(MoneyBalance: StartingMoney, ResourceBalance: StartingResources)),
            (marketMakerAgent, new(MoneyBalance: StartingMoney, ResourceBalance: StartingResources)),
            (momentumAgent, new(MoneyBalance: StartingMoney, ResourceBalance: StartingResources)),
            (spreadSniperAgent, new(MoneyBalance: StartingMoney, ResourceBalance: StartingResources)),
            (randomAgent, new(MoneyBalance: StartingMoney, ResourceBalance: StartingResources)),
            (valueInvestorAgent, new(MoneyBalance: StartingMoney, ResourceBalance: StartingResources)),
        };

        // Add 20 random agents
        for (int i = 0; i < 20; i++)
        {
            var extraRandomAgent = new RandomAgent(maxPrice: 20, random: new Random(i));
            agentInitList.Add((extraRandomAgent, new(MoneyBalance: StartingMoney, ResourceBalance: StartingResources)));
        }

        sim.InitAgents(agentInitList.ToArray());
        var loggingSim = new LoggingSim(sim);

        for (int i = 0; i < 400; i++)
        {
            loggingSim.Tick();
        }

        // Serialize the snapshot log to separate CSV files for money and resources
        var snapshots = loggingSim.GetSnapshots();
        var agentNames = snapshots[0].Keys.OrderBy(name => name).ToList();

        // using (var moneyWriter = new StreamWriter("all_agents_money.csv"))
        // {
        //     moneyWriter.WriteLine("Tick," + string.Join(",", agentNames));
        //     for (int tick = 0; tick < snapshots.Count; tick++)
        //     {
        //         var snapshot = snapshots[tick];
        //         var row = new List<string> { tick.ToString() };
        //         foreach (var agentName in agentNames)
        //         {
        //             row.Add(snapshot[agentName].MoneyBalance.ToString());
        //         }
        //         moneyWriter.WriteLine(string.Join(",", row));
        //     }
        // }

        // using (var resourceWriter = new StreamWriter("all_agents_resources.csv"))
        // {
        //     resourceWriter.WriteLine("Tick," + string.Join(",", agentNames));
        //     for (int tick = 0; tick < snapshots.Count; tick++)
        //     {
        //         var snapshot = snapshots[tick];
        //         var row = new List<string> { tick.ToString() };
        //         foreach (var agentName in agentNames)
        //         {
        //             row.Add(snapshot[agentName].ResourceBalance.ToString());
        //         }
        //         resourceWriter.WriteLine(string.Join(",", row));
        //     }
        // }

        // Generate ScottPlot chart for money over time
        var plot = new Plot();
        double[] ticks = Enumerable.Range(0, snapshots.Count).Select(t => (double)t).ToArray();

        foreach (var agentName in agentNames)
        {
            double[] moneyData = snapshots.Select(s => (double)s[agentName].MoneyBalance).ToArray();
            var scatter = plot.Add.Scatter(ticks, moneyData);
            scatter.LegendText = agentName;
        }

        plot.Title("Agent Money Over Time");
        plot.XLabel("Tick");
        plot.YLabel("Money Balance");
        plot.ShowLegend();
        plot.SavePng("all_agents_money_plot.png", 1200, 800);
    }
}
