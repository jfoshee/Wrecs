using System.Numerics;
using Wrecs.Systems;

namespace Wrecs.Tests;

public class Velocity2DSystemTests
{
    [Fact(DisplayName = "Velocity2D proposes spatial movement each tick")]
    public void Velocity2DProposesSpatialMovementEachTick()
    {
        var spatial2dSystem = new Spatial2DSystem();
        var velocity2dSystem = new Velocity2DSystem();
        var sim = new Sim();
        sim.AddSystems(spatial2dSystem, velocity2dSystem);
        var entity = new TestVelocity2DEntity(EntityId.Next(), "Entity");

        sim.InitEntities((entity, [
            new Spatial2DSnapshot(new Vector2(2, 3)),
            new Velocity2DSnapshot(new Vector2(1, -0.5f))
        ]));

        sim.Tick();
        spatial2dSystem.GetTypedState(entity).Position.Should().Be(new Vector2(3, 2.5f));

        sim.Tick();
        spatial2dSystem.GetTypedState(entity).Position.Should().Be(new Vector2(4, 2));
    }

    [Fact(DisplayName = "Velocity2D apply updates changes future movement")]
    public void Velocity2DApplyUpdatesChangesFutureMovement()
    {
        var spatial2dSystem = new Spatial2DSystem();
        var velocity2dSystem = new Velocity2DSystem();
        var sim = new Sim();
        sim.AddSystems(spatial2dSystem, velocity2dSystem);
        var entity = new TestVelocity2DEntity(EntityId.Next(), "Entity");

        sim.InitEntities((entity, [
            new Spatial2DSnapshot(new Vector2(10, 10)),
            new Velocity2DSnapshot(new Vector2(1, 0))
        ]));

        velocity2dSystem.ApplyUpdates([new Velocity2DUpdate(entity, new Vector2(-2, 5))]);

        sim.Tick();

        spatial2dSystem.GetTypedState(entity).Position.Should().Be(new Vector2(8, 15));
        velocity2dSystem.GetTypedState(entity).Velocity.Should().Be(new Vector2(-2, 5));
    }

    [Fact(DisplayName = "Velocity2D defaults velocity to zero")]
    public void Velocity2DDefaultsVelocityToZero()
    {
        var velocity2dSystem = new Velocity2DSystem();
        var entity = new TestVelocity2DEntity(EntityId.Next(), "Entity");

        velocity2dSystem.InitEntities((entity, null));

        velocity2dSystem.GetTypedState(entity).Velocity.Should().Be(Vector2.Zero);
    }

    [Fact(DisplayName = "Velocity2D requires Spatial2DSystem to propose updates")]
    public void Velocity2DRequiresSpatial2DSystemToProposeUpdates()
    {
        var velocity2dSystem = new Velocity2DSystem();
        var sim = new Sim();
        sim.AddSystem(velocity2dSystem);
        var entity = new TestVelocity2DEntity(EntityId.Next(), "Entity");
        sim.InitEntities((entity, [new Velocity2DSnapshot(new Vector2(1, 1))]));

        Action act = () => sim.Tick();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Spatial2DSystem is required for Velocity2DSystem");
    }

    [Fact(DisplayName = "Velocity2D proposes independent updates for multiple entities")]
    public void Velocity2DProposesIndependentUpdatesForMultipleEntities()
    {
        var spatial2dSystem = new Spatial2DSystem();
        var velocity2dSystem = new Velocity2DSystem();
        var sim = new Sim();
        sim.AddSystems(spatial2dSystem, velocity2dSystem);
        var entityA = new TestVelocity2DEntity(EntityId.Next(), "A");
        var entityB = new TestVelocity2DEntity(EntityId.Next(), "B");

        sim.InitEntities(
            (entityA, [new Spatial2DSnapshot(new Vector2(0, 0)), new Velocity2DSnapshot(new Vector2(1, 2))]),
            (entityB, [new Spatial2DSnapshot(new Vector2(5, -2)), new Velocity2DSnapshot(new Vector2(-3, 1))])
        );

        sim.Tick();

        spatial2dSystem.GetTypedState(entityA).Position.Should().Be(new Vector2(1, 2));
        spatial2dSystem.GetTypedState(entityB).Position.Should().Be(new Vector2(2, -1));
    }

    [Fact(DisplayName = "Velocity2D uses dt when proposing position updates")]
    public void Velocity2DUsesDtWhenProposingPositionUpdates()
    {
        var spatial2dSystem = new Spatial2DSystem();
        var velocity2dSystem = new Velocity2DSystem(dt: 0.25f);
        var sim = new Sim();
        sim.AddSystems(spatial2dSystem, velocity2dSystem);
        var entity = new TestVelocity2DEntity(EntityId.Next(), "Entity");

        sim.InitEntities((entity, [
            new Spatial2DSnapshot(new Vector2(8, -4)),
            new Velocity2DSnapshot(new Vector2(12, 16))
        ]));

        sim.Tick();

        spatial2dSystem.GetTypedState(entity).Position.Should().Be(new Vector2(11, 0));
    }

    [Fact(DisplayName = "Velocity2D updates are isolated per entity")]
    public void Velocity2DUpdatesAreIsolatedPerEntity()
    {
        var spatial2dSystem = new Spatial2DSystem();
        var velocity2dSystem = new Velocity2DSystem();
        var entityA = new TestVelocity2DEntity(EntityId.Next(), "A");
        var entityB = new TestVelocity2DEntity(EntityId.Next(), "B");
        var rejectAConstraint = new RejectEntitySpatialUpdateConstraint(entityA);
        var sim = new Sim();
        sim.AddSystems(spatial2dSystem, velocity2dSystem, rejectAConstraint);

        sim.InitEntities(
            (entityA, [new Spatial2DSnapshot(new Vector2(0, 0)), new Velocity2DSnapshot(new Vector2(10, 0))]),
            (entityB, [new Spatial2DSnapshot(new Vector2(1, 1)), new Velocity2DSnapshot(new Vector2(0, 2))])
        );

        sim.Tick();

        spatial2dSystem.GetTypedState(entityA).Position.Should().Be(new Vector2(0, 0));
        spatial2dSystem.GetTypedState(entityB).Position.Should().Be(new Vector2(1, 3));
    }

    private sealed class RejectEntitySpatialUpdateConstraint(IEntity rejectedEntity) : ISystemConstraint
    {
        public ConstraintResult Validate(UpdateSet candidate) =>
            candidate.Updates
                .OfType<Spatial2DUpdate>()
                .Any(update => update.Entity == rejectedEntity)
                ? ConstraintResult.Reject()
                : ConstraintResult.Accept();
    }

    private record TestVelocity2DEntity(int Id, string Name) : IVelocity2DEntity;
}