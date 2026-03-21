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
        sim.InitAgents((agent1, new AgentState(moneyBalance: 4, resourceBalance: 8)),
                       (agent2, new AgentState(moneyBalance: 3, resourceBalance: 7)));
        sim.InitOffers();

        sim.Tick();

        var state1 = sim.GetState(agent1);
        state1.MoneyBalance.Should().Be(4);
        state1.ResourceBalance.Should().Be(8);
        var state2 = sim.GetState(agent2);
        state2.MoneyBalance.Should().Be(3);
        state2.ResourceBalance.Should().Be(7);
    }
}
