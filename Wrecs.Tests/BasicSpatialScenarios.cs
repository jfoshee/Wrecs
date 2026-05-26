using Wrecs.Systems;

namespace Wrecs.Tests;

public class BasicSpatial1DScenarios
{
    [Fact(DisplayName = "No Agents, Tick Does Nothing")]
    public void NoAgentsTickDoesNothing()
    {
        var sim = new Spatial1DSystem();
        sim.Tick();
    }

    [Fact(DisplayName = "One Stationary Agent Stays Put")]
    public void OneStationaryAgentStaysPut()
    {
        var sim = new Spatial1DSystem();
        var agent = MockSpatial1DAgent(step: 0);
        sim.InitEntities((agent, 5));

        sim.Tick();

        sim.GetTypedState(agent).Position.Should().Be(5);
    }

    [Fact(DisplayName = "One Agent Moves Right")]
    public void OneAgentMovesRight()
    {
        var sim = new Spatial1DSystem();
        var agent = MockSpatial1DAgent(step: 1);
        sim.InitEntities((agent, 0));

        sim.Tick();

        sim.GetTypedState(agent).Position.Should().Be(1);
    }

    [Fact(DisplayName = "One Agent Moves Left")]
    public void OneAgentMovesLeft()
    {
        var sim = new Spatial1DSystem();
        var agent = MockSpatial1DAgent(step: -1);
        sim.InitEntities((agent, 10));

        sim.Tick();

        sim.GetTypedState(agent).Position.Should().Be(9);
    }

    [Fact(DisplayName = "Agent Accumulates Position Over Multiple Ticks")]
    public void AgentAccumulatesPositionOverMultipleTicks()
    {
        var sim = new Spatial1DSystem();
        var agent = MockSpatial1DAgent(step: 3);
        sim.InitEntities((agent, 0));

        sim.Tick();
        sim.Tick();
        sim.Tick();

        sim.GetTypedState(agent).Position.Should().Be(9);
    }

    [Fact(DisplayName = "Two Agents Move Independently")]
    public void TwoAgentsMoveIndependently()
    {
        var sim = new Spatial1DSystem();
        var agent1 = MockSpatial1DAgent(step: 2);
        var agent2 = MockSpatial1DAgent(step: -1);
        sim.InitEntities((agent1, 0), (agent2, 10));

        sim.Tick();

        sim.GetTypedState(agent1).Position.Should().Be(2);
        sim.GetTypedState(agent2).Position.Should().Be(9);
    }

    [Fact(DisplayName = "Agent Receives Current Position In GetStep")]
    public void AgentReceivesCurrentPositionInGetStep()
    {
        var mock = new Mock<ISpatial1DAgent>();
        mock.Setup(a => a.Id).Returns(EntityId.Next());
        mock.Setup(a => a.GetIntent(It.IsAny<int>())).Returns(1);
        var agent = mock.Object;

        var sim = new Spatial1DSystem();
        sim.InitEntities((agent, 7));

        sim.Tick();

        mock.Verify(a => a.GetIntent(7), Times.Once);

        sim.Tick();

        mock.Verify(a => a.GetIntent(8), Times.Once);
    }

    [Fact(DisplayName = "Agent Can Move To Negative Position")]
    public void AgentCanMoveToNegativePosition()
    {
        var sim = new Spatial1DSystem();
        var agent = MockSpatial1DAgent(step: -5);
        sim.InitEntities((agent, 2));

        sim.Tick();

        sim.GetTypedState(agent).Position.Should().Be(-3);
    }

    private static ISpatial1DAgent MockSpatial1DAgent(int step)
    {
        var id = EntityId.Next();
        var mock = new Mock<ISpatial1DAgent>();
        mock.Setup(a => a.Id).Returns(id);
        mock.Setup(a => a.GetIntent(It.IsAny<int>())).Returns(step);
        return mock.Object;
    }
}
