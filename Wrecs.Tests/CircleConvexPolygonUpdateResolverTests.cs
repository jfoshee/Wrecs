using Wrecs.Geometry;
using Wrecs.Systems;

namespace Wrecs.Tests;

public class CircleConvexPolygonUpdateResolverTests
{
    [Fact(DisplayName = "Circle polygon resolver stops before polygon face")]
    public void ResolveUpdates_CardinalCollision_ShortensUpdates()
    {
        var polygon = CreateRectangle(new Vector2(10, -10), new Vector2(20, 10));
        var start = new Circle(new Vector2(1, 1), 2);
        var (resolver, entity) = CreateResolver(start, polygon);
        var destination = start with { Center = new Vector2(21, 1) };
        var unrelatedUpdate = new Spatial1DUpdate(entity, 42);
        var proposed = new UpdateSet([
            new CircleUpdate(entity, destination),
            new Spatial2DUpdate(entity, destination.Center),
            unrelatedUpdate
        ]);

        var result = resolver.ResolveUpdates(proposed);

        result.ConflictResolved.Should().BeTrue();
        var updates = result.UpdateSet.Updates.ToArray();
        var circle = updates.OfType<CircleUpdate>().Single().State.Circle;
        var position = updates.OfType<Spatial2DUpdate>().Single().State.Position;
        var expected = new Vector2(
            10f - start.Radius - CircleConvexPolygonUpdateResolver.CollisionClearance,
            1f);
        circle.Center.X.Should().BeApproximately(expected.X, 0.00001f);
        circle.Center.Y.Should().BeApproximately(expected.Y, 0.00001f);
        position.X.Should().BeApproximately(expected.X, 0.00001f);
        position.Y.Should().BeApproximately(expected.Y, 0.00001f);
        updates.Should().Contain(update => ReferenceEquals(update, unrelatedUpdate));
    }

    [Fact(DisplayName = "Circle polygon resolver slides along polygon edge")]
    public void ResolveUpdates_DiagonalCollision_SlidesAlongEdge()
    {
        var polygon = CreateRectangle(new Vector2(10, -100), new Vector2(20, 100));
        var start = new Circle(new Vector2(1, 1), 2);
        var (resolver, entity) = CreateResolver(start, polygon);
        var destination = start with { Center = new Vector2(21, 21) };

        var result = resolver.ResolveUpdates(new UpdateSet([
            new CircleUpdate(entity, destination),
            new Spatial2DUpdate(entity, destination.Center)
        ]));

        var circle = result.UpdateSet.Updates.OfType<CircleUpdate>().Single().State.Circle;
        var position = result.UpdateSet.Updates.OfType<Spatial2DUpdate>().Single().State.Position;
        var expectedX = 10f - start.Radius -
            CircleConvexPolygonUpdateResolver.CollisionClearance;
        circle.Center.X.Should().BeApproximately(expectedX, 0.00001f);
        position.X.Should().BeApproximately(expectedX, 0.00001f);
        circle.Center.Y.Should().BeApproximately(21f, 0.00001f);
        position.Y.Should().BeApproximately(21f, 0.00001f);
    }

    [Fact(DisplayName = "Circle polygon resolver slide stops at second polygon")]
    public void ResolveUpdates_SlideIntoSecondPolygon_StopsAtSecondPolygon()
    {
        var polygons = new[]
        {
            CreateRectangle(new Vector2(10, -100), new Vector2(20, 100)),
            CreateRectangle(new Vector2(-100, 14), new Vector2(100, 20))
        };
        var start = new Circle(new Vector2(1, 1), 2);
        var (resolver, entity) = CreateResolver(start, polygons);
        var destination = start with { Center = new Vector2(21, 21) };

        var result = resolver.ResolveUpdates(
            new UpdateSet([new CircleUpdate(entity, destination)]));

        var circle = result.UpdateSet.Updates.OfType<CircleUpdate>().Single().State.Circle;
        circle.Center.X.Should().BeApproximately(
            10f - start.Radius - CircleConvexPolygonUpdateResolver.CollisionClearance,
            0.00001f);
        circle.Center.Y.Should().BeApproximately(
            14f - start.Radius - CircleConvexPolygonUpdateResolver.CollisionClearance,
            0.00001f);
    }

    [Fact(DisplayName = "Circle polygon resolver stops before polygon vertex")]
    public void ResolveUpdates_VertexCollision_StopsBeforeVertex()
    {
        var polygon = CreateRectangle(new Vector2(10, 10), new Vector2(20, 20));
        var start = new Circle(new Vector2(1, 1), 2);
        var (resolver, entity) = CreateResolver(start, polygon);
        var destination = start with { Center = new Vector2(21, 21) };

        var result = resolver.ResolveUpdates(
            new UpdateSet([new CircleUpdate(entity, destination)]));

        var circle = result.UpdateSet.Updates.OfType<CircleUpdate>().Single().State.Circle;
        var clearance = CircleConvexPolygonUpdateResolver.CollisionClearance;
        var expectedCoordinate = 10f - (start.Radius + clearance) / MathF.Sqrt(2f);
        circle.Center.X.Should().BeApproximately(expectedCoordinate, 0.00001f);
        circle.Center.Y.Should().BeApproximately(expectedCoordinate, 0.00001f);
    }

    [Fact(DisplayName = "Circle polygon resolver returns original update set when path misses")]
    public void ResolveUpdates_NoCollision_ReturnsOriginalUpdateSet()
    {
        var polygon = CreateRectangle(new Vector2(10, 10), new Vector2(20, 20));
        var start = new Circle(new Vector2(1, 1), 2);
        var (resolver, entity) = CreateResolver(start, polygon);
        var destination = start with { Center = new Vector2(21, 1) };
        var proposed = new UpdateSet([
            new CircleUpdate(entity, destination),
            new Spatial2DUpdate(entity, destination.Center)
        ]);

        var result = resolver.ResolveUpdates(proposed);

        result.ConflictResolved.Should().BeFalse();
        result.UpdateSet.Should().BeSameAs(proposed);
    }

    [Fact(DisplayName = "Circle polygon resolver ignores a polygon on the moving entity")]
    public void ResolveUpdates_SameEntityPolygon_DoesNotCollideWithItself()
    {
        var entity = new TestEntity();
        var start = new Circle(new Vector2(1, 1), 2);
        var destination = start with { Center = new Vector2(5, 1) };
        var circleSystem = new CircleSystem();
        circleSystem.InitEntities((entity, new CircleSnapshot(start)));
        var polygonSystem = new ConvexPolygonSystem();
        polygonSystem.InitEntities((entity, new ConvexPolygonSnapshot(
            CreateRectangle(new Vector2(-10, -10), new Vector2(10, 10)))));
        var resolver = new CircleConvexPolygonUpdateResolver();
        resolver.Inject(circleSystem);
        resolver.Inject(polygonSystem);
        var proposed = new UpdateSet([new CircleUpdate(entity, destination)]);

        var result = resolver.ResolveUpdates(proposed);

        result.ConflictResolved.Should().BeFalse();
        result.UpdateSet.Should().BeSameAs(proposed);
    }

    [Fact(DisplayName = "Circle polygon resolver requires circle and polygon systems")]
    public void ResolveUpdates_MissingDependencies_Throws()
    {
        var resolver = new CircleConvexPolygonUpdateResolver();

        var missingCircleSystem = () => resolver.ResolveUpdates(new UpdateSet([]));

        missingCircleSystem.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{nameof(CircleSystem)}*");

        resolver.Inject(new CircleSystem());
        var missingPolygonSystem = () => resolver.ResolveUpdates(new UpdateSet([]));

        missingPolygonSystem.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{nameof(ConvexPolygonSystem)}*");
    }

    private static (CircleConvexPolygonUpdateResolver Resolver, TestEntity Entity) CreateResolver(
        Circle start,
        params ConvexPolygon[] polygons)
    {
        var entity = new TestEntity();
        var circleSystem = new CircleSystem();
        circleSystem.InitEntities((entity, new CircleSnapshot(start)));

        var polygonSystem = new ConvexPolygonSystem();
        polygonSystem.InitEntities(polygons.Select(polygon =>
            ((IEntity)new TestEntity(), (ConvexPolygonSnapshot?)new(polygon))).ToArray());

        var resolver = new CircleConvexPolygonUpdateResolver();
        resolver.Inject(circleSystem);
        resolver.Inject(polygonSystem);
        return (resolver, entity);
    }

    private static ConvexPolygon CreateRectangle(Vector2 min, Vector2 max) =>
        new([
            new Vector2(min.X, min.Y),
            new Vector2(max.X, min.Y),
            new Vector2(max.X, max.Y),
            new Vector2(min.X, max.Y)
        ]);

    private sealed class TestEntity : IEntity
    {
        public int Id { get; } = EntityId.Next();
        public string Name => nameof(TestEntity);
    }
}
