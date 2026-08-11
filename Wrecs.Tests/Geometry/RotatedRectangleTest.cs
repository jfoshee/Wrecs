using Wrecs.Geometry;

namespace Wrecs.Tests.Geometry;

public class RotatedRectangleTest
{
    /// <summary>
    /// <see href="RotatedRectangleTest.png">RotatedRectangleTest.png</see>
    /// </summary>
    [Fact(DisplayName = "Corners 45")]
    public void Corners_ShouldReturnCorrectCorners()
    {
        // Arrange
        var alignedRectangle = AlignedRectangle.Centered(new(10, 20), new Vector2(7, 4));
        var rotatedRectangle = new RotatedRectangle(alignedRectangle, Angle.ToRadians(45));

        // Act
        var corners = rotatedRectangle.Corners;

        // Assert
        corners.Select(p => p.Round(2)).Should().Equal(
            new Vector2(8.94f, 16.11f),     // BottomLeft
            new Vector2(13.89f, 21.06f),    // BottomRight
            new Vector2(11.06f, 23.89f),    // TopRight
            new Vector2(6.11f, 18.94f)      // TopLeft
        );
    }

    /// <summary>
    /// <see href="RotatedRectangleTest2.png">RotatedRectangleTest.png</see>
    /// </summary>
    [Fact(DisplayName = "Corners 15")]
    public void Corners15()
    {
        // Arrange
        var alignedRectangle = AlignedRectangle.Centered(new(10, 20), new Vector2(7, 4));
        var rotatedRectangle = new RotatedRectangle(alignedRectangle, Angle.ToRadians(15));

        // Act
        var corners = rotatedRectangle.Corners;

        // Assert
        corners.Select(p => p.Round(2)).Should().Equal(
            new Vector2(7.14f, 17.16f),     // BottomLeft
            new Vector2(13.9f, 18.97f),    // BottomRight
            new Vector2(12.86f, 22.84f),    // TopRight
            new Vector2(6.10f, 21.03f)      // TopLeft
        );
    }

    [Fact(DisplayName = "Bounding Rectangle")]
    public void BoundingRect_ShouldReturnCorrectBoundingRect()
    {
        // Arrange
        var alignedRectangle = AlignedRectangle.Centered(new(10, 20), new Vector2(7, 4));
        var rotatedRectangle = new RotatedRectangle(alignedRectangle, Angle.ToRadians(45));

        // Act
        var boundingRect = rotatedRectangle.BoundingRectangle;

        // Assert
        boundingRect.Left.Should().BeApproximately(6.11f, 0.01f);
        boundingRect.Right.Should().BeApproximately(13.89f, 0.01f);
        boundingRect.Bottom.Should().BeApproximately(16.11f, 0.01f);
        boundingRect.Top.Should().BeApproximately(23.89f, 0.01f);
        boundingRect.Center.Should().Be(new Vector2(10, 20));
    }

    [Theory(DisplayName = "Contains Point (Centered and offset)")]
    [InlineData(0, 0, 1, 1, 0, 0, 0, true, "Center of rectangle with no rotation")]
    [InlineData(10, 20, 7, 4, 0, 10, 20, true, "Center of offset rectangle with no rotation")]
    [InlineData(10, 20, 7, 4, 45, 10, 20, true, "Inside center")]
    [InlineData(10, 20, 7, 4, 45, 10, 22, true, "Inside upper right")]
    [InlineData(10, 20, 7, 4, 45, 12, 22, true, "Inside right")]
    [InlineData(10, 20, 7, 4, 45, 12, 20, true, "Inside lower right")]
    [InlineData(10, 20, 7, 4, 45, 10, 18, true, "Inside lower left")]
    [InlineData(10, 20, 7, 4, 45, 8, 18, true, "Inside left")]
    [InlineData(10, 20, 7, 4, 45, 8, 20, true, "Inside upper left")]
    [InlineData(10, 20, 7, 4, 45, 8, 22, false, "Outside above")]
    [InlineData(10, 20, 7, 4, 45, 12, 18, false, "Outside below")]
    [InlineData(10, 20, 7, 4, 45, 6, 16, false, "Outside left")]
    [InlineData(10, 20, 7, 4, 45, 14, 24, false, "Outside right")]
    public void Contains_Centered_Offset(
        float centerX, float centerY, float rectWidth, float rectHeight,
        float rotationDegrees, float pointX, float pointY, bool expected, string scenario)
    {
        // Arrange
        var rotation = Angle.ToRadians(rotationDegrees);
        var center = new Vector2(centerX, centerY);
        var alignedRectangle = AlignedRectangle.Centered(center, rectWidth, rectHeight);
        var rotatedRectangle = new RotatedRectangle(alignedRectangle, rotation);
        var point = new Vector2(pointX, pointY);

        // Act
        var result = rotatedRectangle.Contains(point);

        // Assert
        result.Should().Be(expected, because: scenario);
    }

    [Theory(DisplayName = "Overlaps or touches 2x2 aligned rectangle")]
    [InlineData(6, 16, true, "aligned w/ 1 point inside rotated")]
    [InlineData(8, 16, true, "aligned w/ 2 points inside rotated")]
    [InlineData(10, 16, true, "aligned w/ 1 point inside rotated")]
    [InlineData(12, 16, false, "aligned w/ 0 points inside rotated")]
    [InlineData(6, 18, true, "aligned w/ 2 points inside rotated")]
    [InlineData(8, 18, true, "aligned w/ all 4 points inside rotated")]
    [InlineData(10, 18, true, "aligned w/ 3 points inside rotated")]
    [InlineData(12, 18, true, "aligned w/ 1 points inside rotated")]
    [InlineData(14, 18, false, "aligned w/ 0 points inside rotated")]
    [InlineData(6, 20, true, "aligned w/ 1 point inside rotated")]
    [InlineData(8, 20, true, "aligned w/ 3 points inside rotated")]
    [InlineData(10, 20, true, "aligned w/ all 4 points inside rotated")]
    [InlineData(12, 20, true, "aligned w/ 2 points inside rotated")]
    [InlineData(14, 20, false, "aligned w/ 0 points inside rotated")]
    [InlineData(6, 22, false, "aligned w/ 0 points inside rotated")]
    [InlineData(8, 22, true, "aligned w/ 1 point inside rotated")]
    [InlineData(10, 22, true, "aligned w/ 2 points inside rotated")]
    [InlineData(12, 22, true, "aligned w/ 1 points inside rotated")]
    [InlineData(14, 22, false, "aligned w/ 0 points inside rotated")]
    public void Relation_2x2(float leftX,
                             float bottomY,
                             bool expected,
                             string scenario)
    {
        // Arrange: a 2x2 square at the given position
        var other = new AlignedRectangle(new(leftX, bottomY), 2);
        // Arrange: a 7x4 rectangle centered at 10, 20, rotated 45 degrees
        var alignedRectangle = AlignedRectangle.Centered(new(10, 20), 7, 4);
        var rotatedRectangle = new RotatedRectangle(alignedRectangle, Angle.ToRadians(45));

        // Act
        var result = rotatedRectangle.OverlapsOrTouches(other);

        // Assert
        result.Should().Be(expected, because: scenario);
    }

    [Theory(DisplayName = "Overlaps or touches 4x8 aligned rectangle")]
    [InlineData(6, 16, true, "rotated w/ 2 points inside aligned")]
    [InlineData(12, 16, true, "rotated w/ 1 points inside aligned")]
    public void Relation_4x8(float leftX,
                             float bottomY,
                             bool expected,
                             string scenario)
    {
        // Arrange: a 4x8 rectangle at the given position
        var other = new AlignedRectangle(new(leftX, bottomY), 4, 8);
        // Arrange: a 7x4 rectangle centered at 10, 20, rotated 45 degrees
        var alignedRectangle = AlignedRectangle.Centered(new(10, 20), 7, 4);
        var rotatedRectangle = new RotatedRectangle(alignedRectangle, Angle.ToRadians(45));

        // Act
        var result = rotatedRectangle.OverlapsOrTouches(other);

        // Assert
        result.Should().Be(expected, because: scenario);
    }

    [Theory(DisplayName = "Overlaps or touches 8x8 aligned rectangle")]
    [InlineData(6, 16, true, "rotated w/ all 4 points inside aligned")]
    [InlineData(12, 16, true, "rotated w/ 3 points inside aligned")]
    public void Relation_8x8(float leftX,
                             float bottomY,
                             bool expected,
                             string scenario)
    {
        // Arrange: a 8x8 square at the given position
        var other = new AlignedRectangle(new(leftX, bottomY), 8);
        // Arrange: a 7x4 rectangle centered at 10, 20, rotated 45 degrees
        var alignedRectangle = AlignedRectangle.Centered(new(10, 20), 7, 4);
        var rotatedRectangle = new RotatedRectangle(alignedRectangle, Angle.ToRadians(45));

        // Act
        var result = rotatedRectangle.OverlapsOrTouches(other);

        // Assert
        result.Should().Be(expected, because: scenario);
    }

    [Theory(DisplayName = "Contains Point")]
    // No rotation
    [InlineData(0, 0, 4, 2, 0, 2, 1, true, "Inside rectangle with no rotation")]
    [InlineData(0, 0, 4, 2, 0, 4, 1, true, "On right edge with no rotation")]
    [InlineData(0, 0, 4, 2, 0, 2, 0, true, "On bottom edge with no rotation")]
    [InlineData(0, 0, 4, 2, 0, 5, 2, false, "Outside rectangle on right with no rotation")]
    [InlineData(0, 0, 4, 2, 0, -1, 1, false, "Outside rectangle on left with no rotation")]
    // 90-degree rotation => <1, -1> ... <3, 3>
    [InlineData(0, 0, 4, 2, 90, 1, 1, true, "Inside rectangle with 90-degree rotation")]
    [InlineData(0, 0, 4, 2, 90, 2, 2, true, "On new edge after 90-degree rotation")]
    [InlineData(0, 0, 4, 2, 90, 2, -2, false, "Outside rectangle below with 90-degree rotation")]
    [InlineData(0, 0, 4, 2, 90, 0, 1, false, "Outside rectangle on left with 90-degree rotation")]
    // 45-degree rotation
    [InlineData(0, 0, 4, 2, 45, 1, 1, true, "Inside rectangle with 45-degree rotation")]
    public void Contains_Point(
        float rectX, float rectY, float rectWidth, float rectHeight,
        float rotationDegrees, float pointX, float pointY, bool expected, string scenario)
    {
        // Arrange
        var rotation = Angle.ToRadians(rotationDegrees);
        var alignedRectangle = new AlignedRectangle(new Vector2(rectX, rectY), rectWidth, rectHeight);
        var rotatedRectangle = new RotatedRectangle(alignedRectangle, rotation);
        var point = new Vector2(pointX, pointY);

        // Act
        var result = rotatedRectangle.Contains(point);

        // Assert
        result.Should().Be(expected, because: scenario);
    }

    [Fact(DisplayName = "Dilated by 1 without Rotation")]
    public void Dilate_NoRotation_ShouldReturnCorrectDilatedRectangle()
    {
        // Arrange
        var alignedRectangle = AlignedRectangle.Centered(new(10, 20), 7, 4);
        var rotatedRectangle = new RotatedRectangle(alignedRectangle, rotationRadians: 0);

        // Act
        var dilatedRectangle = rotatedRectangle.Dilate(radius: 1);
        var dilatedAlignedRectangle = dilatedRectangle.OriginalAlignedRectangle;

        // Assert
        dilatedAlignedRectangle.Width.Should().BeApproximately(9, 0.01f);
        dilatedAlignedRectangle.Height.Should().BeApproximately(6, 0.01f);
        dilatedRectangle.Corners.Should().Equal(dilatedAlignedRectangle.Corners);
        dilatedRectangle.Corners.First().Should().Be(new Vector2(5.5f, 17), "BottomLeft");
    }

    /// <summary>
    /// <see href="RotatedRectangleTest2.png">RotatedRectangleTest.png</see>
    /// </summary>
    [Fact(DisplayName = "Dilated by 1 with 15-degree Rotation")]
    public void Dilate_15Degrees_ShouldReturnCorrectDilatedRectangle()
    {
        // Arrange
        var alignedRectangle = AlignedRectangle.Centered(new(10, 20), 7, 4);
        var rotatedRectangle = new RotatedRectangle(alignedRectangle, Angle.ToRadians(15));

        // Act
        var dilatedRectangle = rotatedRectangle.Dilate(radius: 1);
        var dilatedAlignedRectangle = dilatedRectangle.OriginalAlignedRectangle;

        // Assert
        dilatedAlignedRectangle.Width.Should().BeApproximately(9, 0.01f);
        dilatedAlignedRectangle.Height.Should().BeApproximately(6, 0.01f);
        var corners = dilatedRectangle.Corners;
        corners.Select(p => p.Round(2)).Should().Equal(
            new Vector2(6.43f, 15.94f),     // BottomLeft
            new Vector2(15.12f, 18.27f),    // BottomRight
            new Vector2(13.57f, 24.06f),    // TopRight
            new Vector2(4.88f, 21.73f)      // TopLeft
        );
    }

    [Theory(DisplayName = "Dilated Width and Height")]
    [InlineData(10, 20, 7, 4, 0, 1, "Dilate by 1 with no rotation")]
    [InlineData(10, 20, 7, 4, 45, 1, "Dilate by 1 with 45-degree rotation")]
    [InlineData(10, 20, 7, 4, 90, 2, "Dilate by 2 with 90-degree rotation")]
    [InlineData(0, 0, 4, 2, 0, 0.5, "Dilate by 0.5 with no rotation")]
    public void Dilation(
        float centerX, float centerY, float rectWidth, float rectHeight,
        float rotationDegrees, float dilationRadius, string scenario)
    {
        // Arrange
        var rotation = Angle.ToRadians(rotationDegrees);
        var center = new Vector2(centerX, centerY);
        var alignedRectangle = AlignedRectangle.Centered(center, rectWidth, rectHeight);
        var rotatedRectangle = new RotatedRectangle(alignedRectangle, rotation);

        // Act
        var dilatedRectangle = rotatedRectangle.Dilate(dilationRadius);
        var dilatedAlignedRectangle = dilatedRectangle.OriginalAlignedRectangle;

        // Assert
        dilatedRectangle.Center.Should().Be(center, because: "Center should not change in scenario: " + scenario);
        dilatedAlignedRectangle.Width.Should().BeApproximately(rectWidth + 2 * dilationRadius, 0.01f);
        dilatedAlignedRectangle.Height.Should().BeApproximately(rectHeight + 2 * dilationRadius, 0.01f);
    }

    [Theory(DisplayName = "Intersects LineSegment")]
    [InlineData(10, 20, 7, 4, 45, 8, 18, 12, 22, true, "fully contained along major axis")]
    [InlineData(10, 20, 7, 4, 45, 6, 16, 14, 24, true, "endpoints outside thru middle along major axis")]
    [InlineData(10, 20, 7, 4, 45, 6, 24, 14, 16, true, "endpoints outside thru middle along minor axis")]
    [InlineData(10, 20, 7, 4, 45, 8, 22, 12, 18, true, "endpoints outside thru middle along minor axis shorter")]
    [InlineData(10, 20, 7, 4, 45, 6, 16, 8, 18, true, "sw thru left edge")]
    [InlineData(10, 20, 7, 4, 45, 12, 22, 14, 24, true, "ne thru right edge")]
    [InlineData(10, 20, 7, 4, 45, 6, 24, 8, 22, false, "nw above top edge")]
    [InlineData(10, 20, 7, 4, 45, 12, 18, 14, 16, false, "se below bottom edge")]
    public void Intersects_LineSegment(
        float centerX, float centerY, float rectWidth, float rectHeight,
        float rotationDegrees, float startX, float startY, float endX, float endY,
        bool expected, string scenario)
    {
        // Arrange
        var rotation = Angle.ToRadians(rotationDegrees);
        var center = new Vector2(centerX, centerY);
        var alignedRectangle = AlignedRectangle.Centered(center, rectWidth, rectHeight);
        var rotatedRectangle = new RotatedRectangle(alignedRectangle, rotation);
        var lineSegment = new LineSegment(new Vector2(startX, startY), new Vector2(endX, endY));

        // Act
        var result = rotatedRectangle.Intersects(lineSegment);

        // Assert
        result.Should().Be(expected, because: scenario);
    }
}
