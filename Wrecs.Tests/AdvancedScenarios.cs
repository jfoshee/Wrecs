using ScottPlot;

namespace Wrecs.Tests;

public class AdvancedScenarios
{
    [Fact(DisplayName = "One Rich Dumb Agent and One Value Investor Agent")]
    public void OneRichDumbAgentAndOneValueInvestorAgent()
    {
        var sim = new CommercialSim();
        var sellTaker = new AlwaysSellingTaker();
        var buyTaker = new AlwaysBuyingTaker();
        var sellMaker = new AlwaysSellingMaker(10, 1);
        var randomAgent = new RandomAgent(maxPrice: 10);
        var valueInvestor = new ValueInvestorAgent(initialFairPrice: 10,
                                                   maxPosition: 5,
                                                   minCashReserve: 10);
        sim.InitEntities(
            (sellTaker, [new CommercialSnapshot(MoneyBalance: 1_000, ResourceBalance: 1_000)]),
            (buyTaker, [new CommercialSnapshot(MoneyBalance: 1_000, ResourceBalance: 0)]),
            (valueInvestor, [new CommercialSnapshot(MoneyBalance: 50, ResourceBalance: 0)]),
            (randomAgent, [new CommercialSnapshot(MoneyBalance: 1_000, ResourceBalance: 100)]),
            (sellMaker, [
                new CommercialSnapshot(MoneyBalance: 1_000, ResourceBalance: 100),
                new OfferListSnapshot([new(Seller: sellMaker, Buyer: null, Price: 10, Resources: 5)])
            ]));
        var loggingSim = new LoggingSim(sim);

        for (int i = 0; i < 250; i++)
        {
            loggingSim.Tick();
        }

        var valInvestorState = sim.GetCommercialState(valueInvestor);
        valInvestorState.MoneyBalance.Should().BeGreaterThan(100);

        // Serialize the snapshot log to CSV
        var snapshots = loggingSim.GetSnapshots();
        var agentNames = sim.GetAgentNames();
        var sortedIds = agentNames.Keys.OrderBy(id => agentNames[id]).ToList();
        using var writer = new StreamWriter("simulation_log.csv");
        writer.WriteLine("Tick," + string.Join(",", sortedIds.Select(id => $"{agentNames[id]} Money,{agentNames[id]} Resources")));
        for (int tick = 0; tick < snapshots.Count; tick++)
        {
            var snapshot = snapshots[tick];
            var row = new List<string> { tick.ToString() };
            foreach (var id in sortedIds)
            {
                var state = snapshot[id];
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

        var sim = new CommercialSim();

        // One of each agent type from the Agents namespace
        var contrarianAgent = new ContrarianMeanReversionAgent();
        var marketMaker = new InventoryAwareMarketMakerAgent(anchorPrice: 10, targetInventory: 25, inventoryTolerance: 10);
        var marketMaker2 = new InventoryAwareMarketMakerAgent(anchorPrice: 10, targetInventory: 25, inventoryTolerance: 10);
        var marketMaker3 = new InventoryAwareMarketMakerAgent(anchorPrice: 10, targetInventory: 25, inventoryTolerance: 10);
        var marketMaker4 = new InventoryAwareMarketMakerAgent(anchorPrice: 10, targetInventory: 25, inventoryTolerance: 10);
        var momentumAgent = new MomentumChaserAgent();
        var spreadSniperAgent = new SpreadSniperAgent(minProfitPerUnit: 2, maxInventory: 20, minCashReserve: 50);
        var randomAgent = new RandomAgent(maxPrice: 20);
        var valueInvestorAgent = new ValueInvestorAgent(initialFairPrice: 10, maxPosition: 20, minCashReserve: 50);

        var agentInitList = new List<(IEntity, IStateSnapshot[])>
        {
            (contrarianAgent, [new CommercialSnapshot(MoneyBalance: StartingMoney, ResourceBalance: StartingResources)]),
            (marketMaker, [new CommercialSnapshot(MoneyBalance: StartingMoney, ResourceBalance: StartingResources)]),
            (marketMaker2, [new CommercialSnapshot(MoneyBalance: StartingMoney, ResourceBalance: StartingResources)]),
            (marketMaker3, [new CommercialSnapshot(MoneyBalance: StartingMoney, ResourceBalance: StartingResources)]),
            (marketMaker4, [new CommercialSnapshot(MoneyBalance: StartingMoney, ResourceBalance: StartingResources)]),
            (momentumAgent, [new CommercialSnapshot(MoneyBalance: StartingMoney, ResourceBalance: StartingResources)]),
            (spreadSniperAgent, [new CommercialSnapshot(MoneyBalance: StartingMoney, ResourceBalance: StartingResources)]),
            (randomAgent, [new CommercialSnapshot(MoneyBalance: StartingMoney, ResourceBalance: StartingResources)]),
            (valueInvestorAgent, [new CommercialSnapshot(MoneyBalance: StartingMoney, ResourceBalance: StartingResources)]),
        };

        // Add 20 random agents
        for (int i = 0; i < 20; i++)
        {
            var extraRandomAgent = new RandomAgent(maxPrice: 20, random: new Random(i));
            agentInitList.Add((extraRandomAgent, [new CommercialSnapshot(MoneyBalance: StartingMoney, ResourceBalance: StartingResources)]));
        }

        sim.InitEntities([.. agentInitList]);
        var loggingSim = new LoggingSim(sim);

        for (int i = 0; i < 400; i++)
        {
            loggingSim.Tick();
        }

        // Generate ScottPlot chart for money over time
        var snapshots = loggingSim.GetSnapshots();
        var agentNames = sim.GetAgentNames();
        var chartIds = agentNames
            .Where(kvp => !kvp.Value.StartsWith("Random"))
            .OrderBy(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();

        var plot = new Plot();
        var ticks = Enumerable.Range(0, snapshots.Count).Select(t => (double)t).ToArray();

        foreach (var id in chartIds)
        {
            var moneyData = snapshots.Select(s => (double)s[id].MoneyBalance).ToArray();
            var scatter = plot.Add.Scatter(ticks, moneyData);
            scatter.LegendText = agentNames[id];
        }

        plot.Title("Agent Money Over Time");
        plot.XLabel("Tick");
        plot.YLabel("Money Balance");
        plot.ShowLegend();
        plot.SavePng("all_agents_money_plot.png", 1200, 800);
    }
}
