using Wrecs.Geometry;

namespace Wrecs.Tests.Geometry;

public class CircleTests
{
    [Fact(DisplayName = "Circle bounds are centered and sized by radius")]
    public void Bounds_ReturnsEnclosingAlignedRectangle()
    {
        var circle = new Circle(new Vector2(2.5f, -1.5f), 3f);

        circle.Bounds.Should().Be(new AlignedRectangle(
            new Vector2(-0.5f, -4.5f),
            Width: 6f,
            Height: 6f));
    }

    [Theory(DisplayName = "Swept circle intersects axis-aligned segment capsule")]
    [InlineData("Stationary below horizontal segment", 0, 0, Axis2.X, 5, -10, 10, false)]
    [InlineData("Stationary touching horizontal segment", 0, 0, Axis2.X, 4, -10, 10, true)]
    [InlineData("Stationary overlapping horizontal segment", 0, 0, Axis2.X, 3, -10, 10, true)]
    [InlineData("Stationary left of horizontal segment", 0, 0, Axis2.X, 2, 4, 10, false)]
    [InlineData("Stationary right of vertical segment", 0, 0, Axis2.Y, -2, -10, 10, false)]
    [InlineData("Stationary touching vertical segment", 0, 0, Axis2.Y, -1, -10, 10, true)]
    [InlineData("Stationary overlapping vertical segment", 0, 0, Axis2.Y, 0, -10, 10, true)]
    [InlineData("Move right into vertical segment face", 20, 0, Axis2.Y, 10, -10, 10, true)]
    [InlineData("Move right below vertical segment endpoint", 20, 0, Axis2.Y, 10, 5, 10, false)]
    [InlineData("Move right tangent to vertical segment endpoint", 20, 0, Axis2.Y, 10, 4, 10, true)]
    [InlineData("Move right just outside vertical segment endpoint", 20, 0, Axis2.Y, 10, 4.1, 10, false)]
    [InlineData("Move right tangent to horizontal segment face", 20, 0, Axis2.X, 4, 10, 12, true)]
    [InlineData("Move right outside horizontal segment face", 20, 0, Axis2.X, 4.1, 10, 12, false)]
    [InlineData("Move diagonally into vertical segment face", 20, 20, Axis2.Y, 10, -10, 10, true)]
    [InlineData("Move diagonally past short vertical segment", 20, 20, Axis2.Y, 10, 0, 5, false)]
    [InlineData("Move diagonally through degenerate segment", 20, 20, Axis2.Y, 10, 11, 11, true)]
    [InlineData("Move down-left into point at origin", -2, -2, Axis2.X, 0, 0, 0, true)]
    public void TrySweepIntersection_DataCases(string scenario,
                                               float dx,
                                               float dy,
                                               Axis2 segmentAxis,
                                               float segmentIntercept,
                                               float segmentMin,
                                               float segmentMax,
                                               bool expected)
    {
        var start = new Circle(new Vector2(1, 2), 2);
        var destination = start with { Center = start.Center + new Vector2(dx, dy) };
        var segment = new AxisAlignedSegment2(
            segmentAxis,
            new Vector2(segmentIntercept, segmentIntercept),
            new Interval(segmentMin, segmentMax));

        start.TrySweepIntersection(destination, segment, out _)
            .Should().Be(expected, because: scenario);
    }

    [Fact(DisplayName = "Swept circle returns face contact information")]
    public void TrySweepIntersection_FaceHit_ReturnsFirstContact()
    {
        var start = new Circle(Vector2.Zero, 1);
        var destination = start with { Center = new Vector2(10, 0) };
        var wall = new AxisAlignedSegment2(Axis2.Y, new Vector2(6, 0), new Interval(-10, 10));

        var intersects = start.TrySweepIntersection(destination, wall, out var hit);

        intersects.Should().BeTrue();
        hit.Time.Should().BeApproximately(0.5f, 0.00001f);
        // hit.ContactCenter.Should().Be(new Vector2(5, 0));
        hit.Normal.Should().Be(-Vector2.UnitX);
    }

    [Fact(DisplayName = "Swept circle returns radial normal at segment endpoint")]
    public void TrySweepIntersection_EndpointHit_ReturnsRadialNormal()
    {
        var start = new Circle(Vector2.Zero, 1);
        var destination = start with { Center = new Vector2(10, 0) };
        var point = new AxisAlignedSegment2(Axis2.Y, new Vector2(6, 0), new Interval(1, 1));

        var intersects = start.TrySweepIntersection(destination, point, out var hit);

        intersects.Should().BeTrue();
        hit.Time.Should().BeApproximately(0.6f, 0.00001f);
        // hit.ContactCenter.Should().Be(new Vector2(6, 0));
        hit.Normal.Should().Be(-Vector2.UnitY);
    }

    [Fact(DisplayName = "Swept circle can move away from an adjacent segment")]
    public void TrySweepIntersection_MovingAwayFromContact_ReturnsFalse()
    {
        var start = new Circle(Vector2.Zero, 1);
        var destination = start with { Center = new Vector2(-5, 0) };
        var wall = new AxisAlignedSegment2(Axis2.Y, new Vector2(1, 0), new Interval(-10, 10));

        start.TrySweepIntersection(destination, wall, out _).Should().BeFalse();
    }

    [Fact(DisplayName = "Swept circle can move tangent to an adjacent segment")]
    public void TrySweepIntersection_MovingTangentToContact_ReturnsFalse()
    {
        var start = new Circle(Vector2.Zero, 1);
        var destination = start with { Center = new Vector2(0, 5) };
        var wall = new AxisAlignedSegment2(Axis2.Y, new Vector2(1, 0), new Interval(-10, 10));

        start.TrySweepIntersection(destination, wall, out _).Should().BeFalse();
    }

    [Fact(DisplayName = "Swept circle rejects a destination with a different radius")]
    public void TrySweepIntersection_DifferentRadius_ThrowsArgumentException()
    {
        var start = new Circle(Vector2.Zero, 1);
        var destination = new Circle(new Vector2(10, 0), 2);
        var wall = new AxisAlignedSegment2(Axis2.Y, new Vector2(6, 0), new Interval(-10, 10));

        var act = () => start.TrySweepIntersection(destination, wall, out _);

        act.Should().Throw<ArgumentException>().WithParameterName(nameof(destination));
    }

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
            hit.Normal.Round(1).Should().Be(new Vector2(nx.Value, ny.Value));
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

    [Fact(DisplayName = "Circle sliding movement preserves tangent component after wall hit")]
    public void GetAllowedSlidingMovement_DiagonalIntoVerticalWall_SlidesUp()
    {
        var start = new Circle(new Vector2(1, 1), 2);
        var wall = new AxisAlignedSegment2(Axis2.Y, new Vector2(10, 0), new Interval(-10, 50));

        var allowed = start.GetAllowedSlidingMovement(
            new Vector2(20, 20),
            [wall],
            clearance: 0.001f);

        var resolved = start.Center + allowed;
        resolved.X.Should().BeApproximately(10f - 2f - 0.001f, 0.00001f);
        resolved.Y.Should().BeApproximately(21f, 0.00001f);
    }

    [Fact(DisplayName = "Circle sliding movement rechecks collision and stops at second wall")]
    public void GetAllowedSlidingMovement_SlideThenCeilingHit_StopsAtCeiling()
    {
        var start = new Circle(new Vector2(1, 1), 2);
        var walls = new[]
        {
            new AxisAlignedSegment2(Axis2.Y, new Vector2(10, 0), new Interval(-10, 50)),
            new AxisAlignedSegment2(Axis2.X, new Vector2(0, 14), new Interval(-10, 50))
        };

        var allowed = start.GetAllowedSlidingMovement(
            new Vector2(20, 20),
            walls,
            clearance: 0.001f);

        var resolved = start.Center + allowed;
        resolved.X.Should().BeApproximately(10f - 2f - 0.001f, 0.00001f);
        resolved.Y.Should().BeApproximately(14f - 2f - 0.001f, 0.00001f);
    }
}
