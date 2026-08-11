namespace Wrecs.Tests.Geometry;

using static Wrecs.Geometry.SegmentUtilities;

public class PolygonIntersectionTests
{
    [Fact(DisplayName = "Intersecting triangles")]
    public void IntersectingEdges()
    {
        Vector2[] triangle1 =
        [
            new(13, 140),
            new(53, 140),
            new(33, 440)
        ];
        Vector2[] triangle2 =
        [
            new(23, 240),
            new(63, 240),
            new(43, 540)
        ];

        var result = AnyEdgesIntersect(triangle1, triangle2);

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Non-intersecting distinct triangles")]
    public void SeparatedEdges()
    {
        Vector2[] triangle1 =
        [
            new(13, 140),
            new(53, 140),
            new(33, 440)
        ];
        Vector2[] triangle2 =
        [
            new(73, 740),
            new(93, 740),
            new(83, 1040)
        ];

        var result = AnyEdgesIntersect(triangle1, triangle2);

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "Polygons with different vertex counts can have intersecting edges")]
    public void DifferentCounts()
    {
        // Arrange
        Vector2[] triangle =
        [
            new(13, 140),
            new(47, 160),
            new(29, 520)
        ];
        Vector2[] quadrilateral =
        [
            new(25, 110),
            new(38, 130),
            new(35, 610),
            new(23, 570)
        ];

        // Act
        var result = AnyEdgesIntersect(triangle, quadrilateral);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Polygon edge query excludes containment without boundary contact")]
    public void ContainmentOnly()
    {
        Vector2[] outer =
        [
            new(103, 1100),
            new(503, 1100),
            new(503, 2100),
            new(103, 2100)
        ];
        Vector2[] inner =
        [
            new(173, 1310),
            new(311, 1530),
            new(229, 1860)
        ];

        var result = AnyEdgesIntersect(outer, inner);

        result.Should().BeFalse();
    }
}
