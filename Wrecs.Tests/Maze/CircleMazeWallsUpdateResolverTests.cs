using Wrecs.Geometry;
using Wrecs.Systems;

namespace Wrecs.Tests.Maze;

public class CircleMazeWallsUpdateResolverTests
{
    [Theory(DisplayName = "Circle maze wall resolver stops before wall from each direction")]
    [InlineData(Axis2.Y, 10, -20, 20, 21, 1, 7.999f, 1, "Move right")]
    [InlineData(Axis2.Y, -10, -20, 20, -21, 1, -7.999f, 1, "Move left")]
    [InlineData(Axis2.X, 14, -20, 20, 1, 21, 1, 11.999f, "Move up")]
    [InlineData(Axis2.X, -10, -20, 20, 1, -21, 1, -7.999f, "Move down")]
    public void ResolveUpdates_CardinalCollision_ShortenUpdates(
        Axis2 wallAxis,
        float intercept,
        float intervalMin,
        float intervalMax,
        float destinationX,
        float destinationY,
        float expectedX,
        float expectedY,
        string scenario)
    {
        var wall = new AxisAlignedSegment2(
            wallAxis,
            new Vector2(intercept, intercept),
            new Interval(intervalMin, intervalMax));
        var start = new Circle(new Vector2(1, 1), 2);
        var (resolver, entity) = CreateResolver(start, wall);
        var destination = start with { Center = new Vector2(destinationX, destinationY) };
        var unrelatedUpdate = new Spatial1DUpdate(entity, 42);
        var proposed = new UpdateSet([
            new CircleUpdate(entity, destination),
            new Spatial2DUpdate(entity, destination.Center),
            unrelatedUpdate
        ]);

        var result = resolver.ResolveUpdates(proposed);

        result.ConflictResolved.Should().BeTrue(because: scenario);
        var updates = result.UpdateSet.Updates.ToArray();
        var circle = updates.OfType<CircleUpdate>().Single().State.Circle;
        var position = updates.OfType<Spatial2DUpdate>().Single().State.Position;
        circle.Center.X.Should().BeApproximately(expectedX, 0.00001f, because: scenario);
        circle.Center.Y.Should().BeApproximately(expectedY, 0.00001f, because: scenario);
        position.X.Should().BeApproximately(expectedX, 0.00001f, because: scenario);
        position.Y.Should().BeApproximately(expectedY, 0.00001f, because: scenario);
        updates.Should().Contain(update => ReferenceEquals(update, unrelatedUpdate));
    }

    [Fact(DisplayName = "Circle maze wall resolver handles a degenerate segment endpoint")]
    public void ResolveUpdates_PointWall_StopsBeforeEndpoint()
    {
        var point = new AxisAlignedSegment2(Axis2.Y, new Vector2(10, 0), new Interval(1, 1));
        var start = new Circle(new Vector2(1, 1), 2);
        var (resolver, entity) = CreateResolver(start, point);
        var destination = start with { Center = new Vector2(21, 1) };

        var result = resolver.ResolveUpdates(new UpdateSet([new CircleUpdate(entity, destination)]));

        var resolved = result.UpdateSet.Updates.OfType<CircleUpdate>().Single().State.Circle;
        resolved.Center.X.Should().BeApproximately(
            10f - start.Radius - CircleMazeWallsUpdateResolver.CollisionClearance,
            0.00001f);
        resolved.Center.Y.Should().BeApproximately(1f, 0.00001f);
    }

    [Fact(DisplayName = "Circle maze wall resolver uses earliest wall")]
    public void ResolveUpdates_MultipleWalls_UsesEarliestWall()
    {
        var walls = new[]
        {
            new AxisAlignedSegment2(Axis2.Y, new Vector2(15, 0), new Interval(-10, 10)),
            new AxisAlignedSegment2(Axis2.Y, new Vector2(10, 0), new Interval(-10, 10))
        };
        var start = new Circle(new Vector2(1, 1), 2);
        var (resolver, entity) = CreateResolver(start, walls);
        var destination = start with { Center = new Vector2(21, 1) };

        var result = resolver.ResolveUpdates(new UpdateSet([new CircleUpdate(entity, destination)]));

        var circle = result.UpdateSet.Updates.OfType<CircleUpdate>().Single().State.Circle;
        circle.Center.X.Should().BeApproximately(
            10f - start.Radius - CircleMazeWallsUpdateResolver.CollisionClearance,
            0.00001f);
    }

    [Fact(DisplayName = "Circle maze wall resolver returns original update set when path misses")]
    public void ResolveUpdates_NoCollision_ReturnsOriginalUpdateSet()
    {
        var wall = new AxisAlignedSegment2(Axis2.Y, new Vector2(10, 0), new Interval(10, 20));
        var start = new Circle(new Vector2(1, 1), 2);
        var (resolver, entity) = CreateResolver(start, wall);
        var destination = start with { Center = new Vector2(21, 1) };
        var proposed = new UpdateSet([
            new CircleUpdate(entity, destination),
            new Spatial2DUpdate(entity, destination.Center)
        ]);

        var result = resolver.ResolveUpdates(proposed);

        result.ConflictResolved.Should().BeFalse();
        result.UpdateSet.Should().BeSameAs(proposed);
    }

    [Fact(DisplayName = "Circle maze wall resolver slides along wall")]
    public void ResolveUpdates_DiagonalCollision_SlidesAlongWall()
    {
        var wall = new AxisAlignedSegment2(Axis2.Y, new Vector2(10, 0), new Interval(-10, 50));
        var start = new Circle(new Vector2(1, 1), 2);
        var (resolver, entity) = CreateResolver(start, wall);
        var destination = start with { Center = new Vector2(21, 21) };

        var result = resolver.ResolveUpdates(new UpdateSet([
            new CircleUpdate(entity, destination),
            new Spatial2DUpdate(entity, destination.Center)
        ]));

        var circle = result.UpdateSet.Updates.OfType<CircleUpdate>().Single().State.Circle;
        var position = result.UpdateSet.Updates.OfType<Spatial2DUpdate>().Single().State.Position;
        var expectedX = 10f - start.Radius - CircleMazeWallsUpdateResolver.CollisionClearance;
        circle.Center.X.Should().BeApproximately(expectedX, 0.00001f);
        position.X.Should().BeApproximately(expectedX, 0.00001f);
        circle.Center.Y.Should().BeApproximately(21f, 0.00001f);
        position.Y.Should().BeApproximately(21f, 0.00001f);
    }

    [Fact(DisplayName = "Circle maze wall resolver slide stops at second wall")]
    public void ResolveUpdates_SlideIntoSecondWall_StopsAtSecondWall()
    {
        var walls = new[]
        {
            new AxisAlignedSegment2(Axis2.Y, new Vector2(10, 0), new Interval(-10, 50)),
            new AxisAlignedSegment2(Axis2.X, new Vector2(0, 14), new Interval(-10, 50))
        };
        var start = new Circle(new Vector2(1, 1), 2);
        var (resolver, entity) = CreateResolver(start, walls);
        var destination = start with { Center = new Vector2(21, 21) };

        var result = resolver.ResolveUpdates(new UpdateSet([new CircleUpdate(entity, destination)]));

        var circle = result.UpdateSet.Updates.OfType<CircleUpdate>().Single().State.Circle;
        circle.Center.X.Should().BeApproximately(
            10f - start.Radius - CircleMazeWallsUpdateResolver.CollisionClearance,
            0.00001f);
        circle.Center.Y.Should().BeApproximately(
            14f - start.Radius - CircleMazeWallsUpdateResolver.CollisionClearance,
            0.00001f);
    }

    [Fact(DisplayName = "Circle maze wall resolver only changes colliding entities")]
    public void ResolveUpdates_MultipleEntities_OnlyChangesCollidingEntity()
    {
        var wall = new AxisAlignedSegment2(Axis2.Y, new Vector2(10, 0), new Interval(-10, 10));
        var collidingEntity = new TestEntity();
        var passingEntity = new TestEntity();
        var collidingStart = new Circle(new Vector2(1, 1), 2);
        var passingStart = new Circle(new Vector2(1, 20), 2);
        var circleSystem = new CircleSystem();
        circleSystem.InitEntities(
            (collidingEntity, new CircleSnapshot(collidingStart)),
            (passingEntity, new CircleSnapshot(passingStart)));
        var resolver = new CircleMazeWallsUpdateResolver([wall]);
        resolver.Inject(circleSystem);
        var collidingUpdate = new CircleUpdate(
            collidingEntity,
            collidingStart with { Center = new Vector2(21, 1) });
        var passingUpdate = new CircleUpdate(
            passingEntity,
            passingStart with { Center = new Vector2(21, 20) });
        var passingSpatialUpdate = new Spatial2DUpdate(passingEntity, new Vector2(21, 20));

        var result = resolver.ResolveUpdates(new UpdateSet([
            collidingUpdate,
            passingUpdate,
            passingSpatialUpdate
        ]));

        result.ConflictResolved.Should().BeTrue();
        result.UpdateSet.Updates.Should().Contain(update => ReferenceEquals(update, passingUpdate));
        result.UpdateSet.Updates.Should().Contain(update => ReferenceEquals(update, passingSpatialUpdate));
    }

    [Fact(DisplayName = "Circle maze wall resolver requires circle system")]
    public void ResolveUpdates_MissingCircleSystem_Throws()
    {
        var resolver = new CircleMazeWallsUpdateResolver([]);

        var act = () => resolver.ResolveUpdates(new UpdateSet([]));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{nameof(CircleSystem)}*");
    }

    private static (CircleMazeWallsUpdateResolver Resolver, TestEntity Entity) CreateResolver(
        Circle start,
        params AxisAlignedSegment2[] walls)
    {
        var entity = new TestEntity();
        var circleSystem = new CircleSystem();
        circleSystem.InitEntities((entity, new CircleSnapshot(start)));

        var resolver = new CircleMazeWallsUpdateResolver(walls);
        resolver.Inject(circleSystem);
        return (resolver, entity);
    }

    private sealed class TestEntity : IEntity
    {
        public int Id { get; } = EntityId.Next();
        public string Name => nameof(TestEntity);
    }
}
