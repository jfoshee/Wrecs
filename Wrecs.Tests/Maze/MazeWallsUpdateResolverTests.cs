using Wrecs.Geometry;
using Wrecs.Systems;

namespace Wrecs.Tests.Maze;

public class MazeWallsUpdateResolverTests
{
    [Fact(DisplayName = "Maze wall resolver stops rectangle and position before first wall")]
    public void ResolveUpdates_Collision_ShortensRectangleAndPositionUpdates()
    {
        var wall = new AxisAlignedSegment2(Axis2.Y, new(10, 0), new(-10, 10));
        var (resolver, entity) = CreateResolver(wall);
        var destination = new AlignedRectangle(new(21, 1), 2, 2);
        var unrelatedUpdate = new Spatial1DUpdate(entity, 42);
        var proposed = new UpdateSet([
            new AlignedRectangleUpdate(entity, destination),
            new Spatial2DUpdate(entity, destination.BottomLeft),
            unrelatedUpdate
        ]);

        var result = resolver.ResolveUpdates(proposed);

        result.ConflictResolved.Should().BeTrue();
        var resolvedUpdates = result.UpdateSet.Updates.ToArray();
        var rectangle = resolvedUpdates
            .OfType<AlignedRectangleUpdate>()
            .Single()
            .State.Rectangle;
        var position = resolvedUpdates
            .OfType<Spatial2DUpdate>()
            .Single()
            .State.Position;

        var expectedPosition = new Vector2(
            10 - 2 - MazeWallsUpdateResolver.CollisionClearance,
            1);
        rectangle.BottomLeft.X.Should().BeApproximately(expectedPosition.X, 0.00001f);
        rectangle.BottomLeft.Y.Should().BeApproximately(expectedPosition.Y, 0.00001f);
        position.X.Should().BeApproximately(expectedPosition.X, 0.00001f);
        position.Y.Should().BeApproximately(expectedPosition.Y, 0.00001f);
        resolvedUpdates.Should().Contain(update => ReferenceEquals(update, unrelatedUpdate));
    }

    [Fact(DisplayName = "Maze wall resolver uses earliest wall")]
    public void ResolveUpdates_MultipleWalls_StopsBeforeEarliestWall()
    {
        var walls = new[]
        {
            new AxisAlignedSegment2(Axis2.Y, new(15, 0), new(-10, 10)),
            new AxisAlignedSegment2(Axis2.Y, new(10, 0), new(-10, 10))
        };
        var (resolver, entity) = CreateResolver(walls);
        var destination = new AlignedRectangle(new(21, 1), 2, 2);
        var proposed = new UpdateSet([
            new AlignedRectangleUpdate(entity, destination)
        ]);

        var result = resolver.ResolveUpdates(proposed);

        var rectangle = result.UpdateSet.Updates
            .OfType<AlignedRectangleUpdate>()
            .Single()
            .State.Rectangle;
        rectangle.Right.Should().BeApproximately(
            10 - MazeWallsUpdateResolver.CollisionClearance,
            0.00001f);
    }

    [Fact(DisplayName = "Maze wall resolver keeps update set when path misses walls")]
    public void ResolveUpdates_NoCollision_ReturnsOriginalUpdateSet()
    {
        var wall = new AxisAlignedSegment2(Axis2.Y, new(10, 0), new(10, 20));
        var (resolver, entity) = CreateResolver(wall);
        var destination = new AlignedRectangle(new(21, 1), 2, 2);
        var proposed = new UpdateSet([
            new AlignedRectangleUpdate(entity, destination),
            new Spatial2DUpdate(entity, destination.BottomLeft)
        ]);

        var result = resolver.ResolveUpdates(proposed);

        result.ConflictResolved.Should().BeFalse();
        result.UpdateSet.Should().BeSameAs(proposed);
    }

    [Fact(DisplayName = "Maze wall resolver slides along first wall when diagonal movement collides")]
    public void ResolveUpdates_DiagonalIntoVerticalWall_SlidesAlongWall()
    {
        var wall = new AxisAlignedSegment2(Axis2.Y, new(10, 0), new(-10, 50));
        var (resolver, entity) = CreateResolver(wall);
        var destination = new AlignedRectangle(new(21, 21), 2, 2);
        var proposed = new UpdateSet([
            new AlignedRectangleUpdate(entity, destination),
            new Spatial2DUpdate(entity, destination.BottomLeft)
        ]);

        var result = resolver.ResolveUpdates(proposed);

        result.ConflictResolved.Should().BeTrue();

        var rectangle = result.UpdateSet.Updates
            .OfType<AlignedRectangleUpdate>()
            .Single()
            .State.Rectangle;
        var position = result.UpdateSet.Updates
            .OfType<Spatial2DUpdate>()
            .Single()
            .State.Position;

        var expectedX = 10f - 2f - MazeWallsUpdateResolver.CollisionClearance;
        rectangle.BottomLeft.X.Should().BeApproximately(expectedX, 0.00001f);
        position.X.Should().BeApproximately(expectedX, 0.00001f);

        rectangle.BottomLeft.Y.Should().BeApproximately(destination.BottomLeft.Y, 0.00001f);
        position.Y.Should().BeApproximately(destination.BottomLeft.Y, 0.00001f);
    }

    [Fact(DisplayName = "Maze wall resolver slide still stops at second wall")]
    public void ResolveUpdates_DiagonalSlideIntoCeiling_StopsAtCeiling()
    {
        var walls = new[]
        {
            new AxisAlignedSegment2(Axis2.Y, new(10, 0), new(-10, 50)),
            new AxisAlignedSegment2(Axis2.X, new(0, 14), new(-10, 50))
        };
        var (resolver, entity) = CreateResolver(walls);
        var destination = new AlignedRectangle(new(21, 21), 2, 2);
        var proposed = new UpdateSet([
            new AlignedRectangleUpdate(entity, destination),
            new Spatial2DUpdate(entity, destination.BottomLeft)
        ]);

        var result = resolver.ResolveUpdates(proposed);

        result.ConflictResolved.Should().BeTrue();

        var rectangle = result.UpdateSet.Updates
            .OfType<AlignedRectangleUpdate>()
            .Single()
            .State.Rectangle;
        var position = result.UpdateSet.Updates
            .OfType<Spatial2DUpdate>()
            .Single()
            .State.Position;

        var expectedX = 10f - 2f - MazeWallsUpdateResolver.CollisionClearance;
        var expectedY = 14f - 2f - MazeWallsUpdateResolver.CollisionClearance;

        rectangle.BottomLeft.X.Should().BeApproximately(expectedX, 0.00001f);
        position.X.Should().BeApproximately(expectedX, 0.00001f);
        rectangle.BottomLeft.Y.Should().BeApproximately(expectedY, 0.00001f);
        position.Y.Should().BeApproximately(expectedY, 0.00001f);

        rectangle.Right.Should().BeLessThan(10f);
        rectangle.Top.Should().BeLessThan(14f);
    }

    private static (MazeWallsUpdateResolver Resolver, TestEntity Entity) CreateResolver(
        params AxisAlignedSegment2[] walls)
    {
        var entity = new TestEntity();
        var rectangleSystem = new AlignedRectangleSystem();
        rectangleSystem.InitEntities((
            entity,
            new AlignedRectangleSnapshot(new AlignedRectangle(new(1, 1), 2, 2))));

        var resolver = new MazeWallsUpdateResolver(walls);
        resolver.Inject(rectangleSystem);
        return (resolver, entity);
    }

    private sealed class TestEntity : IEntity
    {
        public int Id { get; } = EntityId.Next();
        public string Name => nameof(TestEntity);
    }
}
