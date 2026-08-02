using Wrecs.Geometry;
using Wrecs.Systems;

namespace Wrecs.Tests;

public class AlignedRectangleCollisionEventTests
{
    [Fact(DisplayName = "Overlapping rectangles raise a collision event")]
    public void OverlappingRectanglesRaiseCollisionEvent()
    {
        var entityA = new Entity("A");
        var entityB = new Entity("B");
        var (sim, handler) = CreateSim(
            (entityA, new(new Vector2(0, 0), 2, 2)),
            (entityB, new(new Vector2(1, 1), 2, 2)));

        sim.Tick();

        handler.Events.Should().ContainSingle()
            .Which.Should().Be(new CollisionEvent(entityA, entityB));
    }

    [Theory(DisplayName = "Non-overlapping rectangles do not raise a collision event")]
    [InlineData(3, 0)]
    [InlineData(0, 3)]
    [InlineData(2, 0)]
    [InlineData(0, 2)]
    public void NonOverlappingRectanglesDoNotRaiseCollisionEvent(float x, float y)
    {
        var entityA = new Entity("A");
        var entityB = new Entity("B");
        var (sim, handler) = CreateSim(
            (entityA, new(new Vector2(0, 0), 2, 2)),
            (entityB, new(new Vector2(x, y), 2, 2)));

        sim.Tick();

        handler.Events.Should().BeEmpty();
    }

    [Fact(DisplayName = "Each overlapping rectangle pair raises exactly one collision event")]
    public void EachOverlappingPairRaisesOneCollisionEvent()
    {
        var entityA = new Entity("A");
        var entityB = new Entity("B");
        var entityC = new Entity("C");
        var (sim, handler) = CreateSim(
            (entityA, new(new Vector2(0, 0), 3, 3)),
            (entityB, new(new Vector2(1, 1), 3, 3)),
            (entityC, new(new Vector2(2, 2), 3, 3)));

        sim.Tick();

        handler.Events.Should().BeEquivalentTo(
        [
            new CollisionEvent(entityA, entityB),
            new CollisionEvent(entityA, entityC),
            new CollisionEvent(entityB, entityC)
        ]);
    }

    [Fact(DisplayName = "A persistent overlap raises one collision event per tick")]
    public void PersistentOverlapRaisesOneCollisionEventPerTick()
    {
        var entityA = new Entity("A");
        var entityB = new Entity("B");
        var (sim, handler) = CreateSim(
            (entityA, new(new Vector2(0, 0), 2, 2)),
            (entityB, new(new Vector2(1, 1), 2, 2)));

        sim.Tick();
        sim.Tick();

        handler.Events.Should().Equal(
            new CollisionEvent(entityA, entityB),
            new CollisionEvent(entityA, entityB));
    }

    [Fact(DisplayName = "Preparing collisions without an aligned rectangle system throws")]
    public void MissingAlignedRectangleSystemThrows()
    {
        var system = new AlignedRectangleCollisionEventSystem();

        var act = system.PrepareInternalUpdates;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{nameof(AlignedRectangleSystem)}*");
    }

    private static (Sim Sim, CollisionEventHandler Handler) CreateSim(
        params (IEntity Entity, AlignedRectangle Rectangle)[] entities)
    {
        var rectangleSystem = new AlignedRectangleSystem();
        var collisionSystem = new AlignedRectangleCollisionEventSystem();
        var handler = new CollisionEventHandler();
        var sim = new Sim();
        sim.AddSystems(rectangleSystem, collisionSystem, handler);
        sim.InitEntities(
        [
            .. entities.Select(item =>
                (item.Entity,
                 new IStateSnapshot[]
                 {
                     new AlignedRectangleSnapshot(item.Rectangle)
                 }))
        ]);
        return (sim, handler);
    }

    private sealed class CollisionEventHandler :
        ISystemEventHandler<CollisionEvent>
    {
        public List<CollisionEvent> Events { get; } = [];

        public void HandleTyped(CollisionEvent e) => Events.Add(e);
    }
}
