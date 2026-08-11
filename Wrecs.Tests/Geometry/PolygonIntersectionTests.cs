namespace Wrecs.Tests.Geometry;

using static Wrecs.Geometry.SegmentUtilities;

public class PolygonIntersectionTests
{
    [Fact(DisplayName = "Intersecting triangles")]
    public void AnyEdgesIntersect_IntersectingTriangles_ReturnsTrue()
    {
        // Arrange
        var triangle1 = new[]
        {
            new Vector2(0, 0),
            new Vector2(4, 0),
            new Vector2(2, 3)
        };

        var triangle2 = new[]
        {
            new Vector2(1, 1),
            new Vector2(5, 1),
            new Vector2(3, 4)
        };

        // Act
        var result = AnyEdgesIntersect(triangle1, triangle2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Non-intersecting distinct triangles")]
    public void AnyEdgesIntersect_NonIntersectingTriangles_ReturnsFalse()
    {
        // Arrange
        var triangle1 = new[]
        {
            new Vector2(0, 0),
            new Vector2(4, 0),
            new Vector2(2, 3)
        };

        var triangle2 = new[]
        {
            new Vector2(6, 6),
            new Vector2(8, 6),
            new Vector2(7, 9)
        };

        // Act
        var result = AnyEdgesIntersect(triangle1, triangle2);

        // Assert
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
}
