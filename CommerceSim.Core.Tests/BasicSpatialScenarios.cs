using CommerceSim.Core.Spatial;

namespace CommerceSim.Core.Tests;

public class BasicSpatialScenarios
{
    [Fact(DisplayName = "No Agents, Tick Does Nothing")]
    public void NoAgentsTickDoesNothing()
    {
        var sim = new SpatialSystem();
        sim.Tick();
    }

    [Fact(DisplayName = "One Stationary Agent Stays Put")]
    public void OneStationaryAgentStaysPut()
    {
        var sim = new SpatialSystem();
        var agent = MockSpatialAgent(step: 0);
        sim.InitEntities((agent, 5));

        sim.Tick();

        sim.GetState(agent).Position.Should().Be(5);
    }

    [Fact(DisplayName = "One Agent Moves Right")]
    public void OneAgentMovesRight()
    {
        var sim = new SpatialSystem();
        var agent = MockSpatialAgent(step: 1);
        sim.InitEntities((agent, 0));

        sim.Tick();

        sim.GetState(agent).Position.Should().Be(1);
    }

    [Fact(DisplayName = "One Agent Moves Left")]
    public void OneAgentMovesLeft()
    {
        var sim = new SpatialSystem();
        var agent = MockSpatialAgent(step: -1);
        sim.InitEntities((agent, 10));

        sim.Tick();

        sim.GetState(agent).Position.Should().Be(9);
    }

    [Fact(DisplayName = "Agent Accumulates Position Over Multiple Ticks")]
    public void AgentAccumulatesPositionOverMultipleTicks()
    {
        var sim = new SpatialSystem();
        var agent = MockSpatialAgent(step: 3);
        sim.InitEntities((agent, 0));

        sim.Tick();
        sim.Tick();
        sim.Tick();

        sim.GetState(agent).Position.Should().Be(9);
    }

    [Fact(DisplayName = "Two Agents Move Independently")]
    public void TwoAgentsMoveIndependently()
    {
        var sim = new SpatialSystem();
        var agent1 = MockSpatialAgent(step: 2);
        var agent2 = MockSpatialAgent(step: -1);
        sim.InitEntities((agent1, 0), (agent2, 10));

        sim.Tick();

        sim.GetState(agent1).Position.Should().Be(2);
        sim.GetState(agent2).Position.Should().Be(9);
    }

    [Fact(DisplayName = "Agent Receives Current Position In GetStep")]
    public void AgentReceivesCurrentPositionInGetStep()
    {
        var mock = new Mock<ISpatialAgent>();
        mock.Setup(a => a.Id).Returns(EntityId.Next());
        mock.Setup(a => a.GetStep(It.IsAny<int>())).Returns(1);
        var agent = mock.Object;

        var sim = new SpatialSystem();
        sim.InitEntities((agent, 7));

        sim.Tick();

        mock.Verify(a => a.GetStep(7), Times.Once);

        sim.Tick();

        mock.Verify(a => a.GetStep(8), Times.Once);
    }

    [Fact(DisplayName = "Agent Can Move To Negative Position")]
    public void AgentCanMoveToNegativePosition()
    {
        var sim = new SpatialSystem();
        var agent = MockSpatialAgent(step: -5);
        sim.InitEntities((agent, 2));

        sim.Tick();

        sim.GetState(agent).Position.Should().Be(-3);
    }

    private static ISpatialAgent MockSpatialAgent(int step)
    {
        var id = EntityId.Next();
        var mock = new Mock<ISpatialAgent>();
        mock.Setup(a => a.Id).Returns(id);
        mock.Setup(a => a.GetStep(It.IsAny<int>())).Returns(step);
        return mock.Object;
    }
}
