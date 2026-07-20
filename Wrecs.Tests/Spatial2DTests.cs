using System.Numerics;
using Wrecs.Systems;

namespace Wrecs.Tests;

public class Spatial2DTests
{
    [Fact(DisplayName = "No Agents, Tick Does Nothing")]
    public void NoAgentsTickDoesNothing()
    {
        var s2 = new Spatial2DSystem();
        var sim = new Sim();
        sim.AddSystem(s2);

        sim.Tick();
    }

    [Fact(DisplayName = "One Stationary Agent Stays Put")]
    public void OneStationaryAgentStaysPut()
    {
        var s2 = new Spatial2DSystem();
        var sim = new Sim();
        sim.AddSystem(s2);
        var agent = MockSpatial2DAgent(Vector2.Zero);
        sim.InitEntities((agent, [new Spatial2DSnapshot(new Vector2(3, 5))]));

        sim.Tick();

        s2.GetTypedState(agent).Position.Should().Be(new Vector2(3, 5));
    }

    [Fact(DisplayName = "One Agent Moves On Both Axes")]
    public void OneAgentMovesOnBothAxes()
    {
        var s2 = new Spatial2DSystem();
        var sim = new Sim();
        sim.AddSystem(s2);
        var agent = MockSpatial2DAgent(new Vector2(2, -3));
        sim.InitEntities((agent, [new Spatial2DSnapshot(new Vector2(5, 7))]));

        sim.Tick();

        s2.GetTypedState(agent).Position.Should().Be(new Vector2(7, 4));
    }

    [Fact(DisplayName = "Agent Defaults To Origin")]
    public void AgentDefaultsToOrigin()
    {
        var s2 = new Spatial2DSystem();
        var sim = new Sim();
        sim.AddSystem(s2);
        var agent = MockSpatial2DAgent(new Vector2(1, 1));
        sim.InitEntities((agent, []));

        sim.Tick();

        s2.GetTypedState(agent).Position.Should().Be(new Vector2(1, 1));
    }

    [Fact(DisplayName = "Agent Accumulates Position Over Multiple Ticks")]
    public void AgentAccumulatesPositionOverMultipleTicks()
    {
        var s2 = new Spatial2DSystem();
        var sim = new Sim();
        sim.AddSystem(s2);
        var agent = MockSpatial2DAgent(new Vector2(1.5f, -2));
        sim.InitEntities((agent, []));

        sim.Tick();
        sim.Tick();
        sim.Tick();

        s2.GetTypedState(agent).Position.Should().Be(new Vector2(4.5f, -6));
    }

    [Fact(DisplayName = "Two Agents Move Independently")]
    public void TwoAgentsMoveIndependently()
    {
        var s2 = new Spatial2DSystem();
        var sim = new Sim();
        sim.AddSystem(s2);
        var agent1 = MockSpatial2DAgent(new Vector2(2, 0));
        var agent2 = MockSpatial2DAgent(new Vector2(0, -4));
        sim.InitEntities(
            (agent1, []),
            (agent2, [new Spatial2DSnapshot(new Vector2(10, 10))]));

        sim.Tick();

        s2.GetTypedState(agent1).Position.Should().Be(new Vector2(2, 0));
        s2.GetTypedState(agent2).Position.Should().Be(new Vector2(10, 6));
    }

    [Fact(DisplayName = "Agent Receives Current Position In GetIntent")]
    public void AgentReceivesCurrentPositionInGetIntent()
    {
        var mock = new Mock<ISpatial2DAgent>();
        mock.As<IAgentRequireSnapshot<Spatial2DSnapshot>>();
        mock.Setup(a => a.Id).Returns(EntityId.Next());
        mock.Setup(a => a.GetIntent(It.IsAny<IAgentContext>()))
            .Returns(new AgentIntent(new Move2DAction(new Vector2(1, 2))));
        var agent = mock.Object;

        var s2 = new Spatial2DSystem();
        var sim = new Sim();
        sim.AddSystem(s2);
        sim.InitEntities((agent, [new Spatial2DSnapshot(new Vector2(7, 9))]));

        sim.Tick();

        mock.Verify(a => a.GetIntent(It.Is<IAgentContext>(
            ctx => ctx.GetSnapshot<Spatial2DSnapshot>().Position == new Vector2(7, 9))), Times.Once);

        sim.Tick();

        mock.Verify(a => a.GetIntent(It.Is<IAgentContext>(
            ctx => ctx.GetSnapshot<Spatial2DSnapshot>().Position == new Vector2(8, 11))), Times.Once);
    }

    [Fact(DisplayName = "Apply Updates Sets Position")]
    public void ApplyUpdatesSetsPosition()
    {
        var s2 = new Spatial2DSystem();
        var agent = MockSpatial2DAgent(Vector2.Zero);
        s2.InitEntities((agent, new Spatial2DSnapshot(new Vector2(1, 1))));

        s2.ApplyUpdates([new Spatial2DUpdate(agent, new Vector2(-2, 6))]);

        s2.GetTypedState(agent).Position.Should().Be(new Vector2(-2, 6));
    }

    [Fact(DisplayName = "Translate Intent Rejects Agent Outside System")]
    public void TranslateIntentRejectsAgentOutsideSystem()
    {
        var s2 = new Spatial2DSystem();
        var agent = MockSpatial2DAgent(Vector2.One);
        s2.InitEntities();

        Action act = () => s2.TranslateIntent(agent, new Move2DAction(Vector2.One));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Agent is not part of Spatial2DSystem");
    }

    [Fact(DisplayName = "Distance Uses Euclidean Distance")]
    public void DistanceUsesEuclideanDistance()
    {
        var s2 = new Spatial2DSystem();
        var entity1 = new TestSpatial2DEntity(EntityId.Next(), "Entity1");
        var entity2 = new TestSpatial2DEntity(EntityId.Next(), "Entity2");
        s2.InitEntities(
            (entity1, new Spatial2DSnapshot(new Vector2(1, 1))),
            (entity2, new Spatial2DSnapshot(new Vector2(4, 5))));

        var distance = s2.GetDistance(entity1, entity2);

        distance.Should().Be(5);
    }

    private static ISpatial2DAgent MockSpatial2DAgent(Vector2 step)
    {
        var id = EntityId.Next();
        var mock = new Mock<ISpatial2DAgent>();
        mock.Setup(a => a.Id).Returns(id);
        mock.Setup(a => a.GetIntent(It.IsAny<IAgentContext>())).Returns(new AgentIntent(new Move2DAction(step)));
        return mock.Object;
    }

    private record TestSpatial2DEntity(int Id, string Name) : ISpatial2DEntity;
}
