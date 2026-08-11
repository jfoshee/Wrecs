using Wrecs.Geometry;

namespace Wrecs.Tests.Geometry;

public class LineSegmentTests
{
    [Theory(DisplayName = "Line segment intersection relationship")]
    [InlineData(13, 140, 47, 520, 16, 180, 50, 560, IntersectionRelation.Disjoint, "Separated parallel segments")]
    [InlineData(13, 140, 30, 330, 47, 520, 64, 710, IntersectionRelation.Disjoint, "Separated collinear segments")]
    [InlineData(13, 140, 47, 520, 23, 410, 37, 250, IntersectionRelation.Touching, "Segments cross at one point")]
    [InlineData(13, 140, 47, 520, 47, 520, 59, 240, IntersectionRelation.Touching, "Segments share an endpoint")]
    [InlineData(13, 140, 47, 520, 22, 610, 30, 330, IntersectionRelation.Touching, "Segment ends on other interior")]
    [InlineData(19, 260, 19, 260, 19, 260, 19, 260, IntersectionRelation.Touching, "Identical degenerate points")]
    [InlineData(19, 260, 19, 260, 23, 410, 23, 410, IntersectionRelation.Disjoint, "Distinct degenerate points")]
    [InlineData(30, 330, 30, 330, 13, 140, 47, 520, IntersectionRelation.Touching, "Degenerate point on segment")]
    [InlineData(31, 330, 31, 330, 13, 140, 47, 520, IntersectionRelation.Disjoint, "Degenerate point off segment")]
    [InlineData(13, 140, 53, 140, 31, 70, 31, 420, IntersectionRelation.Touching, "Perpendicular segments cross")]
    [InlineData(13, 140, 53, 140, 17, 240, 67, 240, IntersectionRelation.Disjoint, "Separated horizontal segments")]
    [InlineData(13, 140, 47, 520, 30, 330, 64, 710, IntersectionRelation.Overlapping, "Partial collinear overlap")]
    [InlineData(13, 140, 81, 900, 30, 330, 64, 710, IntersectionRelation.Overlapping, "Collinear containment")]
    [InlineData(13, 140, 47, 520, 47, 520, 13, 140, IntersectionRelation.Overlapping, "Identical reversed segments")]
    public void Relation(float startX,
                         float startY,
                         float endX,
                         float endY,
                         float otherStartX,
                         float otherStartY,
                         float otherEndX,
                         float otherEndY,
                         IntersectionRelation expected,
                         string scenario)
    {
        var segment = new LineSegment(new(startX, startY),
                                      new(endX, endY));
        var other = new LineSegment(new(otherStartX, otherStartY),
                                    new(otherEndX, otherEndY));

        var relation = segment.GetIntersectionRelation(other);

        relation.Should().Be(expected, because: scenario);
        other.GetIntersectionRelation(segment).Should().Be(expected, because: scenario);
        segment.Overlaps(other).Should().Be(expected == IntersectionRelation.Overlapping);
        segment.Touches(other).Should().Be(expected == IntersectionRelation.Touching);
        segment.OverlapsOrTouches(other).Should().Be(expected != IntersectionRelation.Disjoint);
    }

    [Theory(DisplayName = "Large-coordinate near-collinear segments use a scaled tolerance")]
    [InlineData(0.25f, IntersectionRelation.Overlapping, "Sub-tolerance line offset")]
    [InlineData(2f, IntersectionRelation.Disjoint, "Larger line offset")]
    public void NearCollinear(float offsetX,
                              IntersectionRelation expected,
                              string scenario)
    {
        var segment = new LineSegment(new(1_000_013, 2_000_140),
                                      new(1_000_047, 2_000_520));
        var other = new LineSegment(new(1_000_021.5f + offsetX, 2_000_235),
                                    new(1_000_038.5f + offsetX, 2_000_425));

        segment.GetIntersectionRelation(other).Should().Be(expected, because: scenario);
        other.GetIntersectionRelation(segment).Should().Be(expected, because: scenario);
    }

    [Theory(DisplayName = "Large-coordinate collinear endpoint gaps use a scaled tolerance")]
    [InlineData(0.0625f, 0.375f, IntersectionRelation.Touching, "Sub-tolerance endpoint gap")]
    [InlineData(0.25f, 2f, IntersectionRelation.Disjoint, "Larger endpoint gap")]
    public void NearEndpoint(float gapX,
                             float gapY,
                             IntersectionRelation expected,
                             string scenario)
    {
        var segment = new LineSegment(new(1_000_013, 2_000_140),
                                      new(1_000_047, 2_000_520));
        var otherStart = new Vector2(1_000_047 + gapX,
                                     2_000_520 + gapY);
        var other = new LineSegment(otherStart,
                                    otherStart + new Vector2(17, 190));

        segment.GetIntersectionRelation(other).Should().Be(expected, because: scenario);
        other.GetIntersectionRelation(segment).Should().Be(expected, because: scenario);
    }

    [Theory(DisplayName = "Large-coordinate degenerate points use a scaled tolerance")]
    [InlineData(0.25f, IntersectionRelation.Touching, "Sub-tolerance point separation")]
    [InlineData(2f, IntersectionRelation.Disjoint, "Larger point separation")]
    public void NearPoints(float offsetX,
                           IntersectionRelation expected,
                           string scenario)
    {
        var segment = new LineSegment(new(1_000_013, 2_000_140),
                                      new(1_000_013, 2_000_140));
        var other = new LineSegment(new(1_000_013 + offsetX, 2_000_140),
                                    new(1_000_013 + offsetX, 2_000_140));

        segment.GetIntersectionRelation(other).Should().Be(expected, because: scenario);
        other.GetIntersectionRelation(segment).Should().Be(expected, because: scenario);
    }

    [Theory(DisplayName = "Closest point is clamped to the finite line segment")]
    [InlineData(100.5f, 234, 20.5f, 240, "Projection lies within segment")]
    [InlineData(4, 62, 13, 140, "Projection falls before start")]
    [InlineData(73, 920, 43, 540, "Projection falls after end")]
    public void ClosestPoint(float pointX,
                             float pointY,
                             float expectedX,
                             float expectedY,
                             string scenario)
    {
        var segment = new LineSegment(new(13, 140),
                                      new(43, 540));
        var point = new Vector2(pointX, pointY);

        var closestPoint = segment.GetClosestPoint(point);

        closestPoint.Should().Be(new Vector2(expectedX, expectedY), because: scenario);
    }

    [Fact(DisplayName = "Closest point on a zero-length segment is its endpoint")]
    public void ClosestPoint_Degenerate()
    {
        var segment = new LineSegment(new(37, 410),
                                      new(37, 410));

        var closestPoint = segment.GetClosestPoint(new(113, 1270));

        closestPoint.Should().Be(new Vector2(37, 410));
    }
}
