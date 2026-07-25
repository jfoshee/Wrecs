using Wrecs.Geometry;

namespace Wrecs.Tests.Geometry;

public class AlignedRectangleTests
{
    [Fact(DisplayName = "Calculate Center")]
    public void Center_ShouldBeCorrect()
    {
        // Arrange
        var rectangle = new AlignedRectangle(new Vector2(3, 5), 8, 12);

        // Act
        var center = rectangle.Center;

        // Assert
        center.Should().Be(new Vector2(3 + 4, 5 + 6));
    }

    [Fact(DisplayName = "Return Correct Corners")]
    public void GetCorners_ShouldReturnCorrectCorners()
    {
        // Arrange
        var rectangle = new AlignedRectangle(new Vector2(1, 1), 3, 2);

        // Act
        var corners = rectangle.Corners;

        // Assert
        corners.Should().Equal(
            new Vector2(1, 1),    // BottomLeft
            new Vector2(4, 1),    // BottomRight
            new Vector2(4, 3),    // TopRight
            new Vector2(1, 3)     // TopLeft
        );
    }

    [Theory(DisplayName = "Point Containment")]
    [InlineData(0, 0, 4, 4, 2, 2, true, "Inside rectangle")]
    [InlineData(0, 0, 4, 4, 0, 0, true, "On bottom-left corner")]
    [InlineData(0, 0, 4, 4, 4, 4, true, "On top-right corner")]
    [InlineData(0, 0, 4, 4, 4, 2, true, "On right edge")]
    [InlineData(0, 0, 4, 4, 2, 4, true, "On top edge")]
    [InlineData(0, 0, 4, 4, 2, 0, true, "On bottom edge")]
    [InlineData(0, 0, 4, 4, 0, 2, true, "On left edge")]
    [InlineData(0, 0, 4, 4, -1, -1, false, "Outside bottom-left")]
    [InlineData(0, 0, 4, 4, 5, 5, false, "Outside top-right")]
    [InlineData(0, 0, 4, 4, 5, 2, false, "Outside right edge")]
    [InlineData(0, 0, 4, 4, 2, 5, false, "Outside top edge")]
    [InlineData(0, 0, 4, 4, 2, -1, false, "Outside bottom edge")]
    [InlineData(0, 0, 4, 4, -1, 2, false, "Outside left edge")]
    public void Contains_ShouldReturnCorrectResult(
        float rectX, float rectY, float rectWidth, float rectHeight,
        float pointX, float pointY, bool expected, string scenario)
    {
        // Arrange
        var rectangle = new AlignedRectangle(new(rectX, rectY), rectWidth, rectHeight);
        var point = new Vector2(pointX, pointY);

        // Act
        var result = rectangle.Contains(point);

        // Assert
        result.Should().Be(expected, because: scenario);
    }

    [Theory(DisplayName = "Intersection")]
    [InlineData(0, 0, 2, 2, 3, 3, 2, 2, false, "Above and to the right")]
    [InlineData(0, 0, 2, 2, 1, 3, 2, 2, false, "Above")]
    [InlineData(0, 0, 2, 2, -2, 3, 2, 2, false, "Above and to the left")]
    [InlineData(0, 0, 2, 2, 3, 1, 2, 2, false, "Right")]
    [InlineData(0, 0, 2, 2, -3, 1, 2, 2, false, "Left")]
    [InlineData(0, 0, 2, 2, 3, -3, 2, 2, false, "Below and to the right")]
    [InlineData(0, 0, 2, 2, 1, -3, 2, 2, false, "Below")]
    [InlineData(0, 0, 2, 2, -2, -3, 2, 2, false, "Below and to the left")]
    [InlineData(0, 0, 2, 2, 0.5f, 0.5f, 1, 1, true, "Fully contained")]
    [InlineData(0, 0, 2, 2, -1, -1, 4, 4, true, "Overlapping all around")]
    [InlineData(0, 0, 2, 2, 0, 0, 2, 2, true, "Exact match")]
    [InlineData(0, 0, 2, 2, -1, 1, 4, 1, true, "Overlapping above")]
    [InlineData(0, 0, 2, 2, 1, -1, 2, 2, true, "Overlapping below")]
    [InlineData(0, 0, 2, 2, 1, 1, 2, 2, true, "Overlapping top right")]
    [InlineData(0, 0, 2, 2, -1, 0, 4, 2, true, "Overlapping left")]
    [InlineData(0, 0, 2, 2, 1, 1, 2, 2, true, "Overlapping above and to the right")]
    [InlineData(0, 0, 2, 2, -1, 1, 2, 2, true, "Overlapping above and to the left")]
    [InlineData(0, 0, 2, 2, 1, -1, 2, 2, true, "Overlapping below and to the right")]
    [InlineData(0, 0, 2, 2, -1, -1, 2, 2, true, "Overlapping below and to the left")]
    public void Intersects_ShouldReturnExpectedResult(
        float ax, float ay, float aw, float ah,
        float bx, float by, float bw, float bh,
        bool expected, string scenario)
    {
        // Create rectangles
        var a = new AlignedRectangle(new(ax, ay), aw, ah);
        var b = new AlignedRectangle(new(bx, by), bw, bh);

        // Act
        bool resultA = a.Intersects(b);
        bool resultB = b.Intersects(a);

        // Assert
        resultA.Should().Be(expected, because: scenario);
        resultB.Should().Be(expected, because: scenario);
    }

    [Theory(DisplayName = "Axis-aligned segment intersection")]
    [InlineData(3, -2, 4, 6, Axis2.X, 0, 2, 8, true, "Horizontal segment crosses offset rectangle")]
    [InlineData(3, -2, 4, 6, Axis2.X, 0, 4, 6, true, "Horizontal segment is contained")]
    [InlineData(3, -2, 4, 6, Axis2.X, 4, 1, 3, true, "Horizontal endpoint touches top-left corner")]
    [InlineData(3, -2, 4, 6, Axis2.X, -2, 4, 6, true, "Horizontal segment lies on bottom edge")]
    [InlineData(3, -2, 4, 6, Axis2.X, 0, 8, 9, false, "Horizontal segment is beyond right edge")]
    [InlineData(3, -2, 4, 6, Axis2.X, 5, 4, 6, false, "Horizontal segment is above rectangle")]
    [InlineData(3, -2, 4, 6, Axis2.Y, 5, -3, 5, true, "Vertical segment crosses offset rectangle")]
    [InlineData(3, -2, 4, 6, Axis2.Y, 5, -1, 3, true, "Vertical segment is contained")]
    [InlineData(3, -2, 4, 6, Axis2.Y, 7, -4, -2, true, "Vertical endpoint touches bottom-right corner")]
    [InlineData(3, -2, 4, 6, Axis2.Y, 3, -1, 3, true, "Vertical segment lies on left edge")]
    [InlineData(3, -2, 4, 6, Axis2.Y, 5, 5, 6, false, "Vertical segment is above rectangle")]
    [InlineData(3, -2, 4, 6, Axis2.Y, 8, -1, 3, false, "Vertical segment is beyond right edge")]
    [InlineData(0, 0, 4, 4, Axis2.X, 2, -1, 5, true, "Horizontal segment crosses rectangle at origin")]
    public void Intersects_AxisAlignedSegment_ReturnsExpectedResult(
        float rectangleX,
        float rectangleY,
        float rectangleWidth,
        float rectangleHeight,
        Axis2 axis,
        float fixedCoordinate,
        float extentMin,
        float extentMax,
        bool expected,
        string scenario)
    {
        var rectangle = new AlignedRectangle(
            new Vector2(rectangleX, rectangleY),
            rectangleWidth,
            rectangleHeight);
        var anchor = axis switch
        {
            Axis2.X => new Vector2(0, fixedCoordinate),
            Axis2.Y => new Vector2(fixedCoordinate, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(axis))
        };
        var segment = new AxisAlignedSegment2(
            axis,
            anchor,
            new Interval(extentMin, extentMax));

        var result = rectangle.Intersects(segment);

        result.Should().Be(expected, because: scenario);
    }

    [Theory(DisplayName = "Relative Position")]
    [InlineData(0, 0, 2, 2, 3, 3, RelativePosition.Above | RelativePosition.Right, "Above and to the right")]
    [InlineData(0, 0, 2, 2, 1, 3, RelativePosition.Above, "Above")]
    [InlineData(0, 0, 2, 2, -2, 3, RelativePosition.Above | RelativePosition.Left, "Above and to the left")]
    [InlineData(0, 0, 2, 2, 3, 1, RelativePosition.Right, "Right")]
    [InlineData(0, 0, 2, 2, -3, 1, RelativePosition.Left, "Left")]
    [InlineData(0, 0, 2, 2, 3, -3, RelativePosition.Below | RelativePosition.Right, "Below and to the right")]
    [InlineData(0, 0, 2, 2, 1, -3, RelativePosition.Below, "Below")]
    [InlineData(0, 0, 2, 2, -2, -3, RelativePosition.Below | RelativePosition.Left, "Below and to the left")]
    [InlineData(0, 0, 2, 2, 0.5f, 0.5f, RelativePosition.Inside, "Fully contained within")]
    [InlineData(0, 0, 2, 2, 0, 0, RelativePosition.Inside, "On bottom left corner")]
    [InlineData(0, 0, 2, 2, 1, 1, RelativePosition.Inside, "Inside but not on edges")]
    public void GettingRelativePosition(
        float ax, float ay, float aw, float ah,
        float px, float py,
        RelativePosition expected, string scenario)
    {
        // Create the rectangle
        var a = new AlignedRectangle(new(ax, ay), aw, ah);
        var point = new Vector2(px, py);

        // Act
        var result = a.GetRelativePosition(point);

        // Assert
        result.Should().Be(expected, because: scenario);
    }

    [Fact(DisplayName = "Union with Empty Rectangle")]
    public void UnionWithEmptyRectangle_ShouldReturnOriginalRectangle()
    {
        // Arrange
        var rectangle = new AlignedRectangle(new Vector2(1, 1), 3, 2);
        var empty = AlignedRectangle.Empty;

        // Act
        var union1 = rectangle.Union(empty);
        var union2 = empty.Union(rectangle);

        // Assert
        union1.Should().Be(rectangle);
        union2.Should().Be(rectangle);
    }

    [Fact(DisplayName = "Union with Non-Empty Rectangle")]
    public void UnionWithNonEmptyRectangle_ShouldReturnUnion()
    {
        // Arrange
        var rectangle = new AlignedRectangle(new Vector2(1, 1), 3, 2);
        var other = new AlignedRectangle(new Vector2(2, 2), 2, 3);

        // Act
        var union = rectangle.Union(other);

        // Assert
        union.Should().Be(new AlignedRectangle(new Vector2(1, 1), 3, 4));
    }
}
