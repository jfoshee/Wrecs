using Wrecs.Geometry;

namespace Wrecs.Tests.Geometry;

public class CircleConvexPolygonContactTests
{

    [Theory(DisplayName = "Swept circle intersection")]
    // No collision
    [InlineData("Stationary left of polygon", -5, 0, 1, 0, 0, false, null, null, null)]
    [InlineData("Moving up left of polygon", -5, 0, 1, 0, 100, false, null, null, null)]
    [InlineData("Moving left left of polygon", -5, 0, 1, -100, 0, false, null, null, null)]
    // Colliding
    [InlineData("Small circle moves right through left vertical edge, between verts, stopped quarter-way", -3, 0, 0.5f, 2, 0, true, 0.25f, -1f, 0f)] // Left edge at x=-2, y in [-1, 1]
    [InlineData("Large circle moves right through left vertical edge, stopped 6th of way", -6, 0, 2, 12, 0, true, 1f / 6f, -1f, 0f)] // Moving 12 units, stopped at 2 units at x=-2
    [InlineData("Small circle moves up through bottom vertex, stopped 3-quarter-way", 0, -5, 0.5f, 0, 2, true, 0.75f, 0f, -1f)] // Bottom vertex at (0, -3)
    [InlineData("r=2 circle moves up through bottom vertex on left side", -1, -7, 2, 0, 5, true, null, -0.5f, -0.9f)] // Hits bottom vertex slightly before hitting the edge
    [InlineData("r=2 circle moves perpendicular into bottom edge", -4, -5, 1, 4, 4, true, null, -0.7f, -0.7f)] // Hits bottom edge which has 45 degree angle and normal pointing to lower left
    [InlineData("tiny circle moving diagonally into bottom edge", -1.75f, -3.25f, 0.25f, 1, 2, true, null, -0.7f, -0.7f)] // Hits bottom edge which has 45 degree angle and normal pointing to lower left
    [InlineData("Circle on right side moving diagonally into right edge", 5.5f, 0.5f, 1.5f, -3, +1, true, null, 0.9f, -0.3f)] // Hits right edge which has slope of 3
    // Sliding
    [InlineData("Circle sliding down on left edge", -3, 1, 1, 0, -2, false, null, null, null)] // Sliding down along left edge
    // TODO: Circle sliding up on right edge (requires non trivial tangent circle placement)
    public void TrySweepIntersection_ConvexPolygonCases(string scenario,
                                                        float start_x,
                                                        float start_y,
                                                        float radius,
                                                        float dx,
                                                        float dy,
                                                        bool expected,
                                                        float? t,
                                                        float? nx,
                                                        float? ny)
    {
        var start = new Circle(new(start_x, start_y), radius);
        var end = start with { Center = new Vector2(start_x + dx, start_y + dy) };
        // Setup quad: left edge is vertical, top edge has slope of 1/2, bottom edge has slope of -1, right edge has slope of 3
        var polygon = new ConvexPolygon([
            new(-2, 1), new(-2, -1), new(0, -3), new(2, 3)
        ]);

        var intersects = start.TrySweepIntersection(end, polygon, out var hit);

        intersects.Should().Be(expected, scenario);
        if (t.HasValue)
            hit.Time.Should().BeApproximately(t.Value, 0.0001f);
        if (nx.HasValue && ny.HasValue)
        {
            hit.Normal.Length().Should().BeApproximately(1f, 0.0001f);
            hit.Normal.Round(1).Should().Be(new Vector2(nx.Value, ny.Value));
        }
    }

    [Fact(DisplayName = "Swept circle rejects a convex polygon destination with a different radius")]
    public void TrySweepIntersection_ConvexPolygonDifferentRadius_ThrowsArgumentException()
    {
        var start = new Circle(Vector2.Zero, 1);
        var destination = new Circle(new Vector2(10, 0), 2);
        var polygon = new ConvexPolygon([
            new Vector2(4, -2),
            new Vector2(6, -2),
            new Vector2(6, 2),
            new Vector2(4, 2)
        ]);

        var act = () => start.TrySweepIntersection(destination, polygon, out _);

        act.Should().Throw<ArgumentException>().WithParameterName(nameof(destination));
    }

    [Fact(DisplayName = "Stationary circle touching a convex polygon reports immediate contact")]
    public void TrySweepIntersection_ConvexPolygonStationaryContact_ReturnsImmediateHit()
    {
        var start = new Circle(new Vector2(3, 0), 1);
        var polygon = new ConvexPolygon([
            new Vector2(4, -2),
            new Vector2(6, -2),
            new Vector2(6, 2),
            new Vector2(4, 2)
        ]);

        var intersects = start.TrySweepIntersection(start, polygon, out var hit);

        intersects.Should().BeTrue();
        hit.Should().Be(new SweepHit(0f, -Vector2.UnitX));
    }

    [Fact(DisplayName = "Circle touching a convex polygon cannot move inward")]
    public void TrySweepIntersection_ConvexPolygonContactMovingInward_ReturnsImmediateHit()
    {
        var start = new Circle(new Vector2(3, 0), 1);
        var destination = start with { Center = new Vector2(5, 0) };
        var polygon = new ConvexPolygon([
            new Vector2(4, -2),
            new Vector2(6, -2),
            new Vector2(6, 2),
            new Vector2(4, 2)
        ]);

        var intersects = start.TrySweepIntersection(destination, polygon, out var hit);

        intersects.Should().BeTrue();
        hit.Should().Be(new SweepHit(0f, -Vector2.UnitX));
    }

    [Fact(DisplayName = "Circle overlapping a convex polygon from outside reports immediate overlap")]
    public void TrySweepIntersection_ConvexPolygonExternalOverlap_ReturnsImmediateHit()
    {
        var start = new Circle(new Vector2(3.5f, 0), 1);
        var destination = start with { Center = new Vector2(2, 0) };
        var polygon = new ConvexPolygon([
            new Vector2(4, -2),
            new Vector2(6, -2),
            new Vector2(6, 2),
            new Vector2(4, 2)
        ]);

        var intersects = start.TrySweepIntersection(destination, polygon, out var hit);

        intersects.Should().BeTrue();
        hit.Should().Be(new SweepHit(0f, Vector2.Zero));
    }

    [Fact(DisplayName = "Swept circle returns first convex polygon face contact")]
    public void TrySweepIntersection_ConvexPolygonFace_ReturnsFirstContact()
    {
        var start = new Circle(Vector2.Zero, 1);
        var destination = start with { Center = new Vector2(10, 0) };
        var polygon = new ConvexPolygon([
            new Vector2(4, -2),
            new Vector2(6, -2),
            new Vector2(6, 2),
            new Vector2(4, 2)
        ]);

        var intersects = start.TrySweepIntersection(destination, polygon, out var hit);

        intersects.Should().BeTrue();
        hit.Time.Should().BeApproximately(0.3f, 0.00001f);
        hit.Normal.Should().Be(-Vector2.UnitX);
    }

    [Fact(DisplayName = "Swept circle returns radial normal at convex polygon vertex")]
    public void TrySweepIntersection_ConvexPolygonVertex_ReturnsRadialNormal()
    {
        var start = new Circle(new Vector2(0, 3), 1);
        var destination = start with { Center = new Vector2(10, 3) };
        var polygon = new ConvexPolygon([
            new Vector2(4, -2),
            new Vector2(6, -2),
            new Vector2(6, 2),
            new Vector2(4, 2)
        ]);

        var intersects = start.TrySweepIntersection(destination, polygon, out var hit);

        intersects.Should().BeTrue();
        hit.Time.Should().BeApproximately(0.4f, 0.00001f);
        hit.Normal.Should().Be(Vector2.UnitY);
    }

    [Fact(DisplayName = "Swept circle misses a separated convex polygon")]
    public void TrySweepIntersection_ConvexPolygonMiss_ReturnsFalse()
    {
        var start = new Circle(new Vector2(0, 3.1f), 1);
        var destination = start with { Center = new Vector2(10, 3.1f) };
        var polygon = new ConvexPolygon([
            new Vector2(4, -2),
            new Vector2(6, -2),
            new Vector2(6, 2),
            new Vector2(4, 2)
        ]);

        start.TrySweepIntersection(destination, polygon, out _).Should().BeFalse();
    }

    [Fact(DisplayName = "Swept circle reports initial convex polygon overlap")]
    public void TrySweepIntersection_ConvexPolygonInitialOverlap_ReturnsImmediateHit()
    {
        var start = new Circle(new Vector2(5, 0), 1);
        var destination = start with { Center = new Vector2(10, 0) };
        var polygon = new ConvexPolygon([
            new Vector2(4, -2),
            new Vector2(6, -2),
            new Vector2(6, 2),
            new Vector2(4, 2)
        ]);

        var intersects = start.TrySweepIntersection(destination, polygon, out var hit);

        intersects.Should().BeTrue();
        hit.Should().Be(new SweepHit(0f, Vector2.Zero));
    }


    [Fact(DisplayName = "Circle can move away after contacting an axis-aligned polygon edge")]
    public void TrySweepIntersection_AxisAlignedEdgeContact_CanMoveAway()
    {
        var wallCoordinates = new[] { 10f, 10.3f, 0f, -10.3f, -1000f, 1000f };
        var radii = new[] { 0.1f, 0.3f, 1f, 2f, 2.2f, 12f };
        var approaches = new[] { 0.1f, 0.3f, 1f, 7.7f, 20f };

        foreach (var wallX in wallCoordinates)
            foreach (var radius in radii)
                foreach (var approach in approaches)
                {
                    var polygon = CreatePolygon(wallX, 0f);
                    var start = new Circle(new Vector2(wallX - radius - approach, 0f), radius);
                    var destination = start with
                    {
                        Center = start.Center + new Vector2(approach + radius, 0f)
                    };

                    start.TrySweepIntersection(destination, polygon, out var contact).Should().BeTrue();
                    var touching = start with
                    {
                        Center = start.Center + contact.GetAllowedMovement(
                            destination.Center - start.Center)
                    };
                    var movingAway = touching with { Center = touching.Center + contact.Normal };
                    var tangent = new Vector2(-contact.Normal.Y, contact.Normal.X);
                    var movingTangent = touching with { Center = touching.Center + tangent };

                    touching.TrySweepIntersection(movingAway, polygon, out _).Should().BeFalse(
                        $"a radius {radius} circle at x={wallX} should separate after contact");
                    touching.TrySweepIntersection(movingTangent, polygon, out _).Should().BeFalse(
                        $"a radius {radius} circle at x={wallX} should slide after contact");
                }
    }

    [Fact(DisplayName = "Circle can move away after contacting a nearly axis-aligned polygon edge")]
    public void TrySweepIntersection_NearlyAxisAlignedEdgeContact_CanMoveAway()
    {
        var wallCoordinates = new[] { 10f, 10.3f, 0f, -10.3f, -1000f, 1000f };
        var radii = new[] { 0.1f, 0.3f, 1f, 2f, 2.2f, 12f };
        var approaches = new[] { 0.1f, 0.3f, 1f, 7.7f, 20f };
        var tilts = new[] { 0.000001f, 0.00001f, 0.0001f, 0.001f, 0.01f, 0.1f };

        foreach (var wallX in wallCoordinates)
            foreach (var radius in radii)
                foreach (var approach in approaches)
                    foreach (var tilt in tilts)
                    {
                        var polygon = CreatePolygon(wallX, tilt);
                        var facePoint = new Vector2(wallX + tilt / 2f, 0f);
                        var normal = polygon.EdgeNormals[3];
                        var start = new Circle(facePoint + normal * (radius + approach), radius);
                        var destination = start with
                        {
                            Center = start.Center - normal * (approach + radius)
                        };

                        start.TrySweepIntersection(destination, polygon, out var contact).Should().BeTrue();
                        var touching = start with
                        {
                            Center = start.Center + contact.GetAllowedMovement(
                                destination.Center - start.Center)
                        };
                        var movingAway = touching with { Center = touching.Center + contact.Normal };
                        var tangent = new Vector2(-contact.Normal.Y, contact.Normal.X);
                        var movingTangent = touching with { Center = touching.Center + tangent };

                        touching.TrySweepIntersection(movingAway, polygon, out _).Should().BeFalse(
                            $"a radius {radius} circle at x={wallX} with tilt {tilt} should separate after contact");
                        touching.TrySweepIntersection(movingTangent, polygon, out _).Should().BeFalse(
                            $"a radius {radius} circle at x={wallX} with tilt {tilt} should slide after contact");
                    }
    }

    private static ConvexPolygon CreatePolygon(float leftX, float tilt) =>
        new([
            new Vector2(leftX, -100f),
            new Vector2(leftX + 20f, -100f),
            new Vector2(leftX + 20f, 100f),
            new Vector2(leftX + tilt, 100f)
        ]);
}
