namespace Wrecs.Tests;

public class FixedGrantSourceTests
{
    [Fact(DisplayName = "One Source, One Do-Nothing Agent (Money)")]
    public void OneSourceOneDoNothingAgent_Money()
    {
        var sim = new CommercialSimHarness();
        var agent = new DoNothingAgent();
        var source = new FixedGrantSource(agent, money: 16, resources: 0);
        sim.InitEntities((agent, default));
        sim.InitControllers(new MoneySourcesController(source), new ResourceSourcesController(source));

        sim.Tick();

        var snapshot = sim.GetCommercialState(agent);
        snapshot.MoneyBalance.Should().Be(16);
        snapshot.ResourceBalance.Should().Be(0);
    }

    [Fact(DisplayName = "One Source, One Do-Nothing Agent (Resources)")]
    public void OneSourceOneDoNothingAgent_Resources()
    {
        var sim = new CommercialSimHarness();
        var agent = new DoNothingAgent();
        var source = new FixedGrantSource(agent, money: 0, resources: 42);
        sim.InitEntities((agent, default));
        sim.InitControllers(new MoneySourcesController(source), new ResourceSourcesController(source));

        sim.Tick();

        var snapshot = sim.GetCommercialState(agent);
        snapshot.MoneyBalance.Should().Be(0);
        snapshot.ResourceBalance.Should().Be(42);
    }

    [Fact(DisplayName = "One Source, One Do-Nothing Agent, 2 Grants (Money)")]
    public void OneSourceOneDoNothingAgentTwoGrants_Money()
    {
        var sim = new CommercialSimHarness();
        var agent = new DoNothingAgent();
        var source = new FixedGrantSource(agent, money: 10, resources: 0);
        sim.InitEntities((agent, default));
        sim.InitControllers(new MoneySourcesController(source), new ResourceSourcesController(source));

        sim.Tick();
        sim.Tick();

        var snapshot = sim.GetCommercialState(agent);
        snapshot.MoneyBalance.Should().Be(20);
        snapshot.ResourceBalance.Should().Be(0);
    }

    [Fact(DisplayName = "One Source, One Do-Nothing Agent, 2 Grants (Resources)")]
    public void OneSourceOneDoNothingAgentTwoGrants_Resources()
    {
        var sim = new CommercialSimHarness();
        var agent = new DoNothingAgent();
        var source = new FixedGrantSource(agent, money: 0, resources: 300);
        sim.InitEntities((agent, default));
        sim.InitControllers(new MoneySourcesController(source), new ResourceSourcesController(source));

        sim.Tick();
        sim.Tick();

        var snapshot = sim.GetCommercialState(agent);
        snapshot.MoneyBalance.Should().Be(0);
        snapshot.ResourceBalance.Should().Be(600);
    }

    [Fact(DisplayName = "Two Sources, One Agent: Grants Add Up (Money)")]
    public void TwoSourcesOneAgentGrantsAddUp_Money()
    {
        var sim = new CommercialSimHarness();
        var agent = new DoNothingAgent();
        var source1 = new FixedGrantSource(agent, money: 10, resources: 0);
        var source2 = new FixedGrantSource(agent, money: 5, resources: 0);
        sim.InitEntities((agent, default));
        sim.InitControllers(new MoneySourcesController(source1, source2), new ResourceSourcesController(source1, source2));

        sim.Tick();

        var snapshot = sim.GetCommercialState(agent);
        snapshot.MoneyBalance.Should().Be(15);
        snapshot.ResourceBalance.Should().Be(0);
    }

    [Fact(DisplayName = "Two Sources, One Agent: Grants Add Up (Resources)")]
    public void TwoSourcesOneAgentGrantsAddUp_Resources()
    {
        var sim = new CommercialSimHarness();
        var agent = new DoNothingAgent();
        var source1 = new FixedGrantSource(agent, money: 0, resources: 300);
        var source2 = new FixedGrantSource(agent, money: 0, resources: 20);
        sim.InitEntities((agent, default));
        sim.InitControllers(new MoneySourcesController(source1, source2), new ResourceSourcesController(source1, source2));

        sim.Tick();

        var snapshot = sim.GetCommercialState(agent);
        snapshot.MoneyBalance.Should().Be(0);
        snapshot.ResourceBalance.Should().Be(320);
    }

    [Fact(DisplayName = "Two Sources, Two Agents: Grants assigned to correct agent (Money)")]
    public void TwoSourcesTwoAgentsGrantsAssignedToCorrectAgent_Money()
    {
        var sim = new CommercialSimHarness();
        var agent1 = new DoNothingAgent();
        var agent2 = new DoNothingAgent();
        var source1 = new FixedGrantSource(agent1, money: 3, resources: 0);
        var source2 = new FixedGrantSource(agent2, money: 7, resources: 0);
        sim.InitEntities((agent1, default),
                       (agent2, default));
        sim.InitControllers(new MoneySourcesController(source1, source2), new ResourceSourcesController(source1, source2));

        sim.Tick();

        // Verify each agent received the correct grant
        var snapshot1 = sim.GetCommercialState(agent1);
        snapshot1.MoneyBalance.Should().Be(3);
        snapshot1.ResourceBalance.Should().Be(0);
        var snapshot2 = sim.GetCommercialState(agent2);
        snapshot2.MoneyBalance.Should().Be(7);
        snapshot2.ResourceBalance.Should().Be(0);
    }

    [Fact(DisplayName = "Two Sources, Two Agents: Grants assigned to correct agent (Resources)")]
    public void TwoSourcesTwoAgentsGrantsAssignedToCorrectAgent_Resources()
    {
        var sim = new CommercialSimHarness();
        var agent1 = new DoNothingAgent();
        var agent2 = new DoNothingAgent();
        var source1 = new FixedGrantSource(agent1, money: 0, resources: 5);
        var source2 = new FixedGrantSource(agent2, money: 0, resources: 11);
        sim.InitEntities((agent1, default),
                       (agent2, default));
        sim.InitControllers(new MoneySourcesController(source1, source2), new ResourceSourcesController(source1, source2));

        sim.Tick();

        // Verify each agent received the correct grant
        var snapshot1 = sim.GetCommercialState(agent1);
        snapshot1.MoneyBalance.Should().Be(0);
        snapshot1.ResourceBalance.Should().Be(5);
        var snapshot2 = sim.GetCommercialState(agent2);
        snapshot2.MoneyBalance.Should().Be(0);
        snapshot2.ResourceBalance.Should().Be(11);
    }
}
