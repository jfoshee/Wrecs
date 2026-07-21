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
}
