namespace Wrecs.Tests.Geometry;

using static Wrecs.Geometry.SegmentUtilities;

public class SegmentUtilitiesTests
{
    [Theory(DisplayName = "OnSegment: is q between p and r")]
    [InlineData(0, 0, 1, 1, 2, 2, true)]    // q is on the segment from p to r
    [InlineData(0, 0, 2, 2, 1, 1, false)]   // q is outside the segment from p to r
    [InlineData(-1, -1, 0, 0, 1, 1, true)]  // q is on the segment with negative coordinates
    [InlineData(1, 1, 2, 2, 99, 99, true)]  // q is on the segment for a larger range
    // Horizontal line at y = 3
    [InlineData(2, 3, 4, 3, 6, 3, true)]    // q is between p and r
    [InlineData(2, 3, 1, 3, 6, 3, false)]   // q is outside the segment, to the left of p
    [InlineData(2, 3, 7, 3, 6, 3, false)]   // q is outside the segment, to the right of r
    // Vertical line at x = 3
    [InlineData(3, 2, 3, 4, 3, 6, true)]    // q is between p and r
    [InlineData(3, 2, 3, 1, 3, 6, false)]   // q is outside the segment, below p
    [InlineData(3, 2, 3, 7, 3, 6, false)]   // q is outside the segment, above r

    public void OnSegment_ShouldReturnCorrectResult(float px, float py, float qx, float qy, float rx, float ry, bool expected)
    {
        // Arrange
        var p = new Vector2(px, py);
        var q = new Vector2(qx, qy);
        var r = new Vector2(rx, ry);

        // Act
        var result = CollinearOnSegment(p, q, r);

        // Assert
        result.Should().Be(expected);
    }

    [Theory(DisplayName = "Orientation should return the expected result for various point configurations")]
    [InlineData(0, 0, 1, 1, 2, 2, 0)]    // Collinear points on a diagonal line
    [InlineData(1, 1, 2, 2, 3, 3, 0)]    // Collinear points, different values
    [InlineData(-1, -1, 0, 0, 1, 1, 0)]  // Collinear points with negative coordinates
    [InlineData(0, 0, 1, 1, 1, 0, 1)]    // Clockwise, turn right
    [InlineData(1, 1, 2, 2, 3, 1, 1)]    // Clockwise, another configuration
    [InlineData(-1, -1, 0, 0, 1, -1, 1)] // Clockwise, mixed coordinates
    [InlineData(0, 0, 1, 1, 0, 1, -1)]   // Counterclockwise, turn left
    [InlineData(1, 1, 2, 2, 1, 3, -1)]   // Counterclockwise, different values
    [InlineData(-1, -1, 0, 0, -1, 1, -1)]// Counterclockwise, mixed coordinates
    public void Orientation_VariousConfigurations(float px, float py, float qx, float qy, float rx, float ry, float expected)
    {
        // Arrange
        var p = new Vector2(px, py);
        var q = new Vector2(qx, qy);
        var r = new Vector2(rx, ry);

        // Act
        var result = Orientation(p, q, r);

        // Assert
        result.Should().Be(expected);
    }

    [Theory(DisplayName = "Segment Intersection")]
    // General case where the segments intersect
    [InlineData(0, 0, 4, 4, 0, 4, 4, 0, true)]  // Intersection at (2,2)
    [InlineData(1, 1, 3, 3, 2, 2, 5, 5, true)]  // Intersection at (2,2), shared point
    [InlineData(0, 0, 2, 2, 2, 2, 4, 4, true)]  // Intersection at the endpoint

    // Collinear special cases
    [InlineData(0, 0, 4, 4, 1, 1, 3, 3, true)]  // Collinear and overlapping
    [InlineData(0, 0, 4, 4, 5, 5, 6, 6, false)] // Collinear but no overlap
    [InlineData(0, 0, 2, 2, 2, 2, 3, 3, true)]  // Touching at a point (2,2)

    // Non-intersecting lines
    [InlineData(0, 0, 4, 4, 5, 5, 7, 7, false)] // Separate lines, no intersection
    [InlineData(0, 0, 4, 4, 0, 5, 4, 9, false)] // No intersection, distinct lines
    [InlineData(0, 0, 2, 2, 3, 3, 5, 5, false)] // No intersection, separate collinear lines
    [InlineData(2, 3, 5, 3, 6, 3, 8, 3, false)] // horizontal lines, one beside the other
    [InlineData(1, 4, 7, 4, 1, 5, 7, 5, false)] // horizontal lines, different y-values
    [InlineData(3, 2, 3, 5, 3, 6, 3, 8, false)] // vertical lines, same x, one above the other
    [InlineData(4, 1, 4, 7, 5, 1, 5, 7, false)] // vertical lines, different x-values
    public void LinesIntersect_VariousCases(float p1x, float p1y, float q1x, float q1y,
                                            float p2x, float p2y, float q2x, float q2y,
                                            bool expected)
    {
        // Arrange
        var p1 = new Vector2(p1x, p1y);
        var q1 = new Vector2(q1x, q1y);
        var p2 = new Vector2(p2x, p2y);
        var q2 = new Vector2(q2x, q2y);

        // Act
        var result = SegmentsIntersect(p1, q1, p2, q2);

        // Assert
        result.Should().Be(expected);
    }
}
