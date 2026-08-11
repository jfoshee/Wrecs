using Wrecs.Geometry;

namespace Wrecs.Tests.Geometry;

public class RotatedRectangleIntersectionTests
{
    [Theory(DisplayName = "Rotated and aligned rectangles distinguish overlap from touch")]
    [InlineData(167, IntersectionRelation.Disjoint)]
    [InlineData(147, IntersectionRelation.Touching)]
    [InlineData(139, IntersectionRelation.Overlapping)]
    public void AlignedRelation(float otherLeft,
                                IntersectionRelation expected)
    {
        // Arrange
        var rectangle = new RotatedRectangle(new(new(113, 1290), 34, 260),
                                             rotationRadians: 0f);
        var other = new AlignedRectangle(new(otherLeft, 1370), 91, 120);

        // Act
        var relation = rectangle.GetIntersectionRelation(other);

        // Assert
        relation.Should().Be(expected);
        rectangle.Overlaps(other).Should().Be(expected == IntersectionRelation.Overlapping);
        rectangle.Touches(other).Should().Be(expected == IntersectionRelation.Touching);
        rectangle.OverlapsOrTouches(other).Should().Be(expected != IntersectionRelation.Disjoint);
    }

    [Theory(DisplayName = "Two rotated rectangles distinguish overlap from separation")]
    [InlineData(143, 1460, IntersectionRelation.Overlapping)]
    [InlineData(317, 1960, IntersectionRelation.Disjoint)]
    public void RotatedRelation(float otherCenterX,
                                float otherCenterY,
                                IntersectionRelation expected)
    {
        // Arrange
        var rectangle = new RotatedRectangle(AlignedRectangle.Centered(new(137, 1420),
                                                                        34,
                                                                        260),
                                             Angle.ToRadians(17));
        var other = new RotatedRectangle(AlignedRectangle.Centered(new(otherCenterX,
                                                                        otherCenterY),
                                                                    51,
                                                                    120),
                                         Angle.ToRadians(-11));

        // Act
        var relation = rectangle.GetIntersectionRelation(other);

        // Assert
        relation.Should().Be(expected);
        rectangle.Overlaps(other).Should().Be(expected == IntersectionRelation.Overlapping);
        rectangle.Touches(other).Should().Be(expected == IntersectionRelation.Touching);
        rectangle.OverlapsOrTouches(other).Should().Be(expected != IntersectionRelation.Disjoint);
    }

    [Fact(DisplayName = "Rotated rectangles classify a shared edge as touching")]
    public void SharedEdge()
    {
        // Arrange
        var rectangle = new RotatedRectangle(new(new(113, 1290), 34, 260),
                                             rotationRadians: 0f);
        var other = new RotatedRectangle(new(new(147, 1370), 91, 120),
                                         rotationRadians: 0f);

        // Act
        var relation = rectangle.GetIntersectionRelation(other);

        // Assert
        relation.Should().Be(IntersectionRelation.Touching);
        rectangle.Touches(other).Should().BeTrue();
        rectangle.Overlaps(other).Should().BeFalse();
    }

    [Fact(DisplayName = "Minimum translation separates a contained rectangle")]
    public void ContainmentTranslation()
    {
        // Arrange
        var inner = new RotatedRectangle(new(new(120, 1200), 40, 100),
                                         rotationRadians: 0f);
        var outer = new RotatedRectangle(new(new(100, 1000), 200, 600),
                                         rotationRadians: 0f);

        // Act
        var overlaps = inner.Overlaps(outer, out var minimumTranslation);
        var translatedRectangle = inner.OriginalAlignedRectangle with
        {
            BottomLeft = inner.OriginalAlignedRectangle.BottomLeft + minimumTranslation
        };
        var translated = new RotatedRectangle(translatedRectangle,
                                              inner.RotationRadians);

        // Assert
        overlaps.Should().BeTrue();
        minimumTranslation.Should().Be(new Vector2(-60, 0));
        translated.GetIntersectionRelation(outer).Should().Be(IntersectionRelation.Touching);
    }

    [Fact(DisplayName = "Closest boundary point preserves the point's edge coordinate")]
    public void ClosestBoundary()
    {
        // Arrange
        var rectangle = new RotatedRectangle(new(new(113, 1290), 34, 260),
                                             rotationRadians: 0f);
        var point = new Vector2(203, 1417);

        // Act
        var closest = rectangle.GetClosestPointOnEdge(point);

        // Assert
        closest.Should().Be(new Vector2(147, 1417));
    }

    [Theory(DisplayName = "Rotated rectangle and line segment distinguish overlap from touch")]
    [InlineData(100, 1427, 180, 1427, IntersectionRelation.Overlapping)]
    [InlineData(154, 1327, 154, 1513, IntersectionRelation.Touching)]
    [InlineData(173, 1327, 173, 1513, IntersectionRelation.Disjoint)]
    public void SegmentRelation(float startX,
                                float startY,
                                float endX,
                                float endY,
                                IntersectionRelation expected)
    {
        // Arrange
        var aligned = AlignedRectangle.Centered(new(137, 1420),
                                                34,
                                                260);
        var angle = Angle.ToRadians(17);
        var rectangle = new RotatedRectangle(aligned, angle);
        var rotation = Matrix3x2.CreateRotation(angle, rectangle.Center);
        var segment = new LineSegment(Vector2.Transform(new(startX, startY), rotation),
                                      Vector2.Transform(new(endX, endY), rotation));

        // Act
        var relation = rectangle.GetIntersectionRelation(segment);

        // Assert
        relation.Should().Be(expected);
        rectangle.Overlaps(segment).Should().Be(expected == IntersectionRelation.Overlapping);
        rectangle.Touches(segment).Should().Be(expected == IntersectionRelation.Touching);
        rectangle.OverlapsOrTouches(segment).Should().Be(expected != IntersectionRelation.Disjoint);
    }
}
