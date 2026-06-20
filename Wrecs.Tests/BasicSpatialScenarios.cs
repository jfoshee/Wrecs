using Wrecs.Systems;

namespace Wrecs.Tests;

public class BasicSpatial1DScenarios
{
    [Fact(DisplayName = "No Agents, Tick Does Nothing")]
    public void NoAgentsTickDoesNothing()
    {
        var s1 = new Spatial1DSystem();
        var sim = new Sim();
        sim.AddSystem(s1);
        sim.Tick();
    }

    [Fact(DisplayName = "One Stationary Agent Stays Put")]
    public void OneStationaryAgentStaysPut()
    {
        var s1 = new Spatial1DSystem();
        var sim = new Sim();
        sim.AddSystem(s1);
        var agent = MockSpatial1DAgent(step: 0);
        sim.InitEntities((agent, [new Spatial1DSnapshot(5)]));

        sim.Tick();

        s1.GetTypedState(agent).Position.Should().Be(5);
    }

    [Fact(DisplayName = "One Agent Moves Right")]
    public void OneAgentMovesRight()
    {
        var s1 = new Spatial1DSystem();
        var sim = new Sim();
        sim.AddSystem(s1);
        var agent = MockSpatial1DAgent(step: 1);
        sim.InitEntities((agent, []));

        sim.Tick();

        s1.GetTypedState(agent).Position.Should().Be(1);
    }

    [Fact(DisplayName = "One Agent Moves Left")]
    public void OneAgentMovesLeft()
    {
        var s1 = new Spatial1DSystem();
        var sim = new Sim();
        sim.AddSystem(s1);
        var agent = MockSpatial1DAgent(step: -1);
        sim.InitEntities((agent, [new Spatial1DSnapshot(10)]));

        sim.Tick();

        s1.GetTypedState(agent).Position.Should().Be(9);
    }

    [Fact(DisplayName = "Agent Accumulates Position Over Multiple Ticks")]
    public void AgentAccumulatesPositionOverMultipleTicks()
    {
        var s1 = new Spatial1DSystem();
        var sim = new Sim();
        sim.AddSystem(s1);
        var agent = MockSpatial1DAgent(step: 3);
        sim.InitEntities((agent, []));

        sim.Tick();
        sim.Tick();
        sim.Tick();

        s1.GetTypedState(agent).Position.Should().Be(9);
    }

    [Fact(DisplayName = "Two Agents Move Independently")]
    public void TwoAgentsMoveIndependently()
    {
        var s1 = new Spatial1DSystem();
        var sim = new Sim();
        sim.AddSystem(s1);
        var agent1 = MockSpatial1DAgent(step: 2);
        var agent2 = MockSpatial1DAgent(step: -1);
        sim.InitEntities((agent1, []), (agent2, [new Spatial1DSnapshot(10)]));

        sim.Tick();

        s1.GetTypedState(agent1).Position.Should().Be(2);
        s1.GetTypedState(agent2).Position.Should().Be(9);
    }

    [Fact(DisplayName = "Agent Receives Current Position In GetStep")]
    public void AgentReceivesCurrentPositionInGetStep()
    {
        var mock = new Mock<ISpatial1DAgent>();
        mock.As<IRequireSnapshot<Spatial1DSnapshot>>();
        mock.Setup(a => a.Id).Returns(EntityId.Next());
        mock.Setup(a => a.GetIntent(It.IsAny<IAgentContext>())).Returns(new AgentIntent(new Move1DAction(1)));
        var agent = mock.Object;

        var s1 = new Spatial1DSystem();
        var sim = new Sim();
        sim.AddSystem(s1);
        sim.InitEntities((agent, [new Spatial1DSnapshot(7)]));

        sim.Tick();

        mock.Verify(a => a.GetIntent(It.Is<IAgentContext>(ctx => ctx.GetSnapshot<Spatial1DSnapshot>().Position == 7)), Times.Once);

        sim.Tick();

        mock.Verify(a => a.GetIntent(It.Is<IAgentContext>(ctx => ctx.GetSnapshot<Spatial1DSnapshot>().Position == 8)), Times.Once);
    }

    [Fact(DisplayName = "Agent Can Move To Negative Position")]
    public void AgentCanMoveToNegativePosition()
    {
        var s1 = new Spatial1DSystem();
        var sim = new Sim();
        sim.AddSystem(s1);
        var agent = MockSpatial1DAgent(step: -5);
        sim.InitEntities((agent, [new Spatial1DSnapshot(2)]));

        sim.Tick();

        s1.GetTypedState(agent).Position.Should().Be(-3);
    }

    private static ISpatial1DAgent MockSpatial1DAgent(int step)
    {
        var id = EntityId.Next();
        var mock = new Mock<ISpatial1DAgent>();
        mock.Setup(a => a.Id).Returns(id);
        mock.Setup(a => a.GetIntent(It.IsAny<IAgentContext>())).Returns(new AgentIntent(new Move1DAction(step)));
        return mock.Object;
    }
}
