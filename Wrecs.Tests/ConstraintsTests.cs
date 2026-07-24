using Wrecs.Geometry;
using Wrecs.Systems;

namespace Wrecs.Tests;

public class ConstraintsTests
{
    [Fact(DisplayName = "ConstraintResult.Accept() returns a valid result with no events")]
    public void TestConstraintAccept()
    {
        var result = ConstraintResult.Accept();
        result.IsValid.Should().BeTrue();
        result.Events.Should().BeEmpty();
    }

    public class TestEvent : IEvent;

    [Fact(DisplayName = "ConstraintResult.Reject() returns an invalid result with the specified events")]
    public void TestConstraintReject()
    {
        var event1 = new TestEvent();
        var event2 = new TestEvent();
        var result = ConstraintResult.Reject(event1, event2);

        result.IsValid.Should().BeFalse();
        result.Events.Should().HaveCount(2);
        result.Events.Should().Contain(event1);
        result.Events.Should().Contain(event2);
    }

    class PositivePositionConstraint : ISystemConstraint
    {
        public ConstraintResult Validate(UpdateSet candidate)
        {
            foreach (var update in candidate.Updates)
            {
                if (update is Spatial1DUpdate spatialUpdate && spatialUpdate.State.Position < 0)
                {
                    return ConstraintResult.Reject(new TestEvent());
                }
            }
            return ConstraintResult.Accept();
        }
    }

    [Fact(DisplayName = "Constraint prevents negative 1D positions")]
    public void PreventNegativePositions()
    {
        // Setup 1D Spatial system with a constraint that prevents negative positions
        var sim = new Sim();
        var s1 = new Spatial1DSystem();
        var constraint = new PositivePositionConstraint();
        sim.AddSystems(s1, constraint);
        // Setup an Agent that starts at X = 2 and moves left by 1 each tick
        var agent = BasicSpatial1DScenarios.MockSpatial1DAgent(step: -1);
        sim.InitEntities((agent, [new Spatial1DSnapshot(2)]));

        // Tick 1: Agent moves to X = 1
        sim.Tick();
        s1.GetTypedState(agent).Position.Should().Be(1);

        // Tick 2: Agent moves to X = 0
        sim.Tick();
        s1.GetTypedState(agent).Position.Should().Be(0);

        // Tick 3: Agent attempts to move to X = -1, but constraint prevents it
        sim.Tick();
        s1.GetTypedState(agent).Position.Should().Be(0, "the constraint should prevent the agent from moving to a negative position");
    }

    [Fact(DisplayName = "Constraint raises events when an update is rejected")]
    public void ConstraintRaisesEventsOnRejection()
    {
        // Setup 1D Spatial system with a constraint that prevents negative positions
        var sim = new Sim();
        var s1 = new Spatial1DSystem();
        var handler = new Mock<ISystemEventHandler<TestEvent>>();
        var constraint = new PositivePositionConstraint();
        sim.AddSystems(s1, constraint, handler.Object);
        // Setup an Agent that starts at X = 0 and moves left by 1 each tick
        var agent = BasicSpatial1DScenarios.MockSpatial1DAgent(step: -1);
        sim.InitEntities((agent, [new Spatial1DSnapshot(0)]));

        // Tick 1: Agent attempts to move to X = -1, but constraint prevents it
        sim.Tick();
        s1.GetTypedState(agent).Position.Should().Be(0, "the constraint should prevent the agent from moving to a negative position");
        handler.Verify(h => h.Handle(It.IsAny<TestEvent>()), Times.Once);
    }

    [Fact(DisplayName = "Constraint rejection rejects all updates derived from one intent action")]
    public void ConstraintRejectionRejectsAllUpdatesFromOneIntentAction()
    {
        var sim = new Sim();
        var spatial = new Spatial2DSystem();
        var rectangles = new AlignedRectangleSystem();
        var constraint = new Positive2DPositionConstraint();
        sim.AddSystems(spatial, rectangles, constraint);
        var agent = MockSpatial2DAgent(new Move2DAction(new Vector2(-1, 0)));
        var initialRectangle = AlignedRectangle.UnitSquare;
        sim.InitEntities((agent, [
            new Spatial2DSnapshot(Vector2.Zero),
            new AlignedRectangleSnapshot(initialRectangle)
        ]));

        sim.Tick();

        spatial.GetTypedState(agent).Position.Should().Be(
            Vector2.Zero,
            "the rejected action must not update any translating system");
        rectangles.GetTypedState(agent).Rectangle.Should().Be(
            initialRectangle,
            "updates derived from the same action are applied atomically");
    }

    [Fact(DisplayName = "Each action in an agent intent is constrained independently")]
    public void EachActionInIntentIsConstrainedIndependently()
    {
        var sim = new Sim();
        var spatial = new Spatial2DSystem();
        var rectangles = new AlignedRectangleSystem();
        var constraint = new Positive2DPositionConstraint();
        sim.AddSystems(spatial, rectangles, constraint);
        var acceptedStep = new Vector2(1, 0);
        var agent = MockSpatial2DAgent(
            new Move2DAction(acceptedStep),
            new Move2DAction(new Vector2(-1, 0)));
        var initialRectangle = AlignedRectangle.UnitSquare;
        sim.InitEntities((agent, [
            new Spatial2DSnapshot(Vector2.Zero),
            new AlignedRectangleSnapshot(initialRectangle)
        ]));

        sim.Tick();

        spatial.GetTypedState(agent).Position.Should().Be(acceptedStep);
        rectangles.GetTypedState(agent).Rectangle.BottomLeft.Should().Be(
            initialRectangle.BottomLeft + acceptedStep);
    }

    [Fact(DisplayName = "Disabling a constraint allows previously rejected updates to succeed")]
    public void DisablingConstraintAllowsUpdates()
    {
        var sim = new Sim();
        var s1 = new Spatial1DSystem();
        var constraint = new PositivePositionConstraint();
        sim.AddSystems(s1, constraint);
        var agent = BasicSpatial1DScenarios.MockSpatial1DAgent(step: -1);
        sim.InitEntities((agent, [new Spatial1DSnapshot(0)]));

        // Tick 1: Agent attempts to move to X = -1, but constraint prevents it
        sim.Tick();
        s1.GetTypedState(agent).Position.Should().Be(0);

        // Disable the constraint
        sim.DisableSystem<PositivePositionConstraint>();

        // Tick 2: Agent should now be able to move to X = -1
        sim.Tick();
        s1.GetTypedState(agent).Position.Should().Be(-1);
    }

    [Fact(DisplayName = "Re-enabling a constraint enforces it again")]
    public void ReEnablingConstraintEnforcesIt()
    {
        var sim = new Sim();
        var s1 = new Spatial1DSystem();
        var constraint = new PositivePositionConstraint();
        sim.AddSystems(s1, constraint);
        var agent = BasicSpatial1DScenarios.MockSpatial1DAgent(step: -1);
        sim.InitEntities((agent, [new Spatial1DSnapshot(0)]));

        // Tick 1: Agent attempts to move to X = -1, but constraint prevents it
        sim.Tick();
        s1.GetTypedState(agent).Position.Should().Be(0);

        // Disable the constraint
        sim.DisableSystem<PositivePositionConstraint>();

        // Tick 2: Agent should now be able to move to X = -1
        sim.Tick();
        s1.GetTypedState(agent).Position.Should().Be(-1);

        // Re-enable the constraint
        sim.EnableSystem<PositivePositionConstraint>();

        // Tick 3: Agent attempts to move to X = -2, but constraint prevents it
        sim.Tick();
        s1.GetTypedState(agent).Position.Should().Be(-1);
    }

    private sealed class Positive2DPositionConstraint : ISystemConstraint
    {
        public ConstraintResult Validate(UpdateSet candidate) =>
            candidate.Updates
                .OfType<Spatial2DUpdate>()
                .Any(update => update.State.Position.X < 0)
                ? ConstraintResult.Reject()
                : ConstraintResult.Accept();
    }

    private static ISpatial2DAgent MockSpatial2DAgent(params Move2DAction[] actions)
    {
        var mock = new Mock<ISpatial2DAgent>();
        mock.Setup(agent => agent.Id).Returns(EntityId.Next());
        mock.Setup(agent => agent.GetIntent(It.IsAny<IAgentContext>()))
            .Returns(new AgentIntent(actions.Cast<IAgentIntentAction>()));
        return mock.Object;
    }
}
