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
}
