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
