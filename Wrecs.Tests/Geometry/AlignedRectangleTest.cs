using Wrecs.Geometry;

namespace Wrecs.Tests.Geometry;

public class AlignedRectangleTests
{
    [Fact(DisplayName = "Construction")]
    public void ARect_Construction()
    {
        var rect = new AlignedRectangle(new(1, 2), 3, 4);

        rect.Left.Should().Be(1);
        rect.Bottom.Should().Be(2);
        rect.Width.Should().Be(3);
        rect.Height.Should().Be(4);
    }

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
    public void Intersects_AxisAlignedSegment_ReturnsExpectedResult(float rectangleX,
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
        var rectangle = new AlignedRectangle(new(rectangleX, rectangleY),
                                             rectangleWidth,
                                             rectangleHeight);
        var anchor = axis switch
        {
            Axis2.X => new Vector2(0, fixedCoordinate),
            Axis2.Y => new Vector2(fixedCoordinate, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(axis))
        };
        var segment = new AxisAlignedSegment2(axis,
                                              anchor,
                                              new(extentMin, extentMax));

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
        var rectangle = new AlignedRectangle(new(1, 1), 3, 2);
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
        var rectangle = new AlignedRectangle(new(1, 1), 3, 2);
        var other = new AlignedRectangle(new(2, 2), 2, 3);

        // Act
        var union = rectangle.Union(other);

        // Assert
        union.Should().Be(new AlignedRectangle(new(1, 1), 3, 4));
    }

    [Theory(DisplayName = "Swept path includes both rectangles and the space between them")]
    [InlineData(2.5f, -3.25f, 4.5f, 2.75f, 11.25f, -3.25f, 2.5f, -3.25f, 13.25f, 2.75f,
        "Horizontal translation to the right")]
    [InlineData(-1.75f, 6.5f, 3.25f, 5.5f, -9.5f, 6.5f, -9.5f, 6.5f, 11, 5.5f,
        "Horizontal translation to the left")]
    [InlineData(7.25f, -4.5f, 6.75f, 3.5f, 7.25f, 8.25f, 7.25f, -4.5f, 6.75f, 16.25f,
        "Vertical translation upward")]
    [InlineData(-8.5f, 9.75f, 2.25f, 4.75f, -8.5f, -6.25f, -8.5f, -6.25f, 2.25f, 20.75f,
        "Vertical translation downward")]
    [InlineData(3.5f, 2.25f, 4.25f, 6.75f, 7.75f, 2.25f, 3.5f, 2.25f, 8.5f, 6.75f,
        "Destination touches the starting right edge")]
    public void Sweep_ReturnsExpectedRectangleInBothDirections(float startX,
                                                               float startY,
                                                               float width,
                                                               float height,
                                                               float destinationX,
                                                               float destinationY,
                                                               float expectedX,
                                                               float expectedY,
                                                               float expectedWidth,
                                                               float expectedHeight,
                                                               string scenario)
    {
        var start = new AlignedRectangle(new(startX, startY), width, height);
        var destination = new AlignedRectangle(new(destinationX, destinationY),
                                               width,
                                               height);
        var expected = new AlignedRectangle(new(expectedX, expectedY),
                                            expectedWidth,
                                            expectedHeight);

        var forward = start.Sweep(destination);
        var reverse = destination.Sweep(start);

        forward.Should().Be(expected, because: scenario);
        reverse.Should().Be(expected, because: $"{scenario}; the operation is commutative");
    }

    [Fact(DisplayName = "Swept path to the same place is the starting rectangle")]
    public void Sweep_SameStartAndDestination_ReturnsStartingRectangle()
    {
        var rectangle = new AlignedRectangle(new(-3.25f, 8.5f), 5.75f, 2.5f);

        var result = rectangle.Sweep(rectangle);

        result.Should().Be(rectangle);
    }

    [Theory(DisplayName = "Swept path supports zero-area rectangles")]
    [InlineData(2, 3, 0, 4, 9, 3, 2, 3, 7, 4, "Zero width")]
    [InlineData(-5, 6, 3, 0, -5, -2, -5, -2, 3, 8, "Zero height")]
    [InlineData(0, 0, 0, 0, 5, 0, 0, 0, 5, 0, "Point swept horizontally")]
    public void Sweep_ZeroAreaRectangle_ReturnsExpectedRectangle(float startX,
                                                                 float startY,
                                                                 float width,
                                                                 float height,
                                                                 float destinationX,
                                                                 float destinationY,
                                                                 float expectedX,
                                                                 float expectedY,
                                                                 float expectedWidth,
                                                                 float expectedHeight,
                                                                 string scenario)
    {
        var start = new AlignedRectangle(new(startX, startY), width, height);
        var destination = new AlignedRectangle(new(destinationX, destinationY),
                                               width,
                                               height);
        var expected = new AlignedRectangle(
            new(expectedX, expectedY),
            expectedWidth,
            expectedHeight);

        start.Sweep(destination).Should().Be(expected, because: scenario);
        destination.Sweep(start).Should().Be(
            expected,
            because: $"{scenario}; the operation is commutative");
    }

    [Theory(DisplayName = "Swept path rejects destinations that are not translations on one axis")]
    [InlineData(1, 2, 4, 5, 7, 9, 4, 5, "Diagonal movement")]
    [InlineData(1, 2, 4, 5, 7, 2, 6, 5, "Width changed")]
    [InlineData(1, 2, 4, 5, 1, 9, 4, 3, "Height changed")]
    public void Sweep_InvalidDestination_ThrowsArgumentException(float startX,
                                                                 float startY,
                                                                 float startWidth,
                                                                 float startHeight,
                                                                 float destinationX,
                                                                 float destinationY,
                                                                 float destinationWidth,
                                                                 float destinationHeight,
                                                                 string scenario)
    {
        var start = new AlignedRectangle(new(startX, startY),
                                         startWidth,
                                         startHeight);
        var destination = new AlignedRectangle(new(destinationX, destinationY),
                                               destinationWidth,
                                               destinationHeight);

        var act = () => start.Sweep(destination);

        act.Should()
           .Throw<ArgumentException>(because: scenario)
           .WithParameterName(nameof(destination));
    }

    [Theory(DisplayName = "Swept 3x5 rectangle intersecting axis aligned segment")]
    [InlineData("No movement; Horizontal Segment below", 0, 0, Axis2.X, 0, -10, 10, false)] // bottom of rect is at y=2
    [InlineData("No movement; Horizontal Segment above", 0, 0, Axis2.X, 8, -10, 10, false)] // top of rect is at y=2+5=7
    [InlineData("No movement; Horizontal Segment left", 0, 0, Axis2.X, 5, -10, 0, false)] // left of rect is at x=1
    [InlineData("No movement; Horizontal Segment right", 0, 0, Axis2.X, 5, 5, 10, false)] // right of rect is at x=1+3=4
    [InlineData("No movement; Horizontal Segment intersects completely", 0, 0, Axis2.X, 5, -10, 10, true)] // y=5 is between 2 and 7, crosses both sides
    [InlineData("No movement; Vertical Segment left", 0, 0, Axis2.Y, 0, -10, 10, false)] // left of rect is at x=1
    [InlineData("No movement; Vertical Segment right", 0, 0, Axis2.Y, 5, -10, 10, false)] // right of rect is at x=1+3=4
    [InlineData("No movement; Vertical Segment below", 0, 0, Axis2.Y, 2, -10, 1, false)] // bottom of rect is at y=2
    [InlineData("No movement; Vertical Segment above", 0, 0, Axis2.Y, 2, 8, 20, false)] // top of rect is at y=2+5=7
    [InlineData("No movement; Vertical Segment intersects completely", 0, 0, Axis2.Y, 2, -10, 10, true)] // x=2 is between 1 and 4, crosses both sides
    [InlineData("Move right by 20; Horizontal Segment below", 20, 0, Axis2.X, 0, -10, 10, false)] // bottom of rect remains at y=2
    [InlineData("Move right by 20; Horizontal Segment above", 20, 0, Axis2.X, 8, -10, 10, false)] // top of rect remains at y=7
    [InlineData("Move right by 20; Horizontal Segment left", 20, 0, Axis2.X, 5, -10, 0, false)] // left of rect moves from x=1 to x=21
    [InlineData("Move right by 20; Horizontal Segment right", 20, 0, Axis2.X, 5, 25, 30, false)] // right of rect moves from x=4 to x=24
    [InlineData("Move right by 20; Horizontal Segment intersects completely", 20, 0, Axis2.X, 5, -30, 30, true)] // y=5 is between 2 and 7, crosses both sides through entire path
    [InlineData("Move right by 20; Horizontal Segment intersects destination right", 20, 0, Axis2.X, 5, 22, 25, true)] // y=5 is between 2 and 7, crosses right side of destination at x=21 to x=24
    [InlineData("Move right by 20; Horizontal Segment intersects path between start and destination", 20, 0, Axis2.X, 5, 13, 14, true)] // y=5 is between 2 and 7, crosses path between x=4 and x=21
    [InlineData("Move right by 20; Vertical Segment left", 20, 0, Axis2.Y, 0, -10, 10, false)] // x=0, left of rect moves from x=1 to x=21
    [InlineData("Move right by 20; Vertical Segment right", 20, 0, Axis2.Y, 25, -10, 10, false)] // x=25, right of rect moves from x=4 to x=24
    [InlineData("Move right by 20; Vertical Segment below", 20, 0, Axis2.Y, 2, -10, 1, false)] // bottom of rect remains at y=2
    [InlineData("Move right by 20; Vertical Segment above", 20, 0, Axis2.Y, 2, 8, 20, false)] // top of rect remains at y=7
    [InlineData("Move right by 20; Vertical Segment intersects destination top", 20, 0, Axis2.Y, 22, 6, 8, true)] // x=22 is between dest left=21 and right=24, crosses top which remains at y=7
    [InlineData("Move up by 20; Horizontal Segment below", 0, 20, Axis2.X, 0, -10, 10, false)] // bottom of rect moves from y=2 to y=22
    [InlineData("Move up by 20; Horizontal Segment above", 0, 20, Axis2.X, 28, -10, 10, false)] // top of rect moves from y=7 to y=27
    [InlineData("Move up by 20; Horizontal Segment intersects destination left", 0, 20, Axis2.X, 26, -1, 2, true)] // y=26 is between dest bottom=22 and top=27, crosses left which remains at x=1
    [InlineData("Move up by 20; Horizontal Segment intersects path between start and destination", 0, 20, Axis2.X, 11, 2, 3, true)] // y=11 is between starting top=7 and destination bottom=22, x=2..3 is completely inside the rectangle's swept volume
    [InlineData("Move up-right by (20, 20); Horizontal Segment above", 20, 20, Axis2.X, 28, -10, 10, false)] // top of rect moves from y=7 to y=27
    [InlineData("Move up-right by (20, 20); Vertical Segment right", 20, 20, Axis2.Y, 25, -10, 10, false)] // right of rect moves from x=4 to x=24
    [InlineData("Move up-right by (20, 20); Horizontal Segment right (at prior y)", 20, 20, Axis2.X, 5, 22, 25, false)] // this segment intersected when only moving horizontally, but now the rectangle is moving up as well, so it is no longer at y=5
    [InlineData("Move up-right by (20, 20); Vertical Segment above (at prior x)", 20, 20, Axis2.Y, 22, 6, 8, false)] // this segment intersected when only moving vertically, but now the rectangle is moving right as well, so it is no longer at x=22
    [InlineData("Move up-right by (20, 20); Horizontal Segment intersects path between start and destination", 20, 20, Axis2.X, 11, -100, 100, true)] // y=11 is between starting top=7 and destination bottom=22, x=-100..100 is completely outside the rectangle's ultimate left and right
    [InlineData("Move up-right by (20, 20); Vertical Segment intersects path between start and destination", 20, 20, Axis2.Y, 22, -100, 100, true)] // x=22 is between starting right=4 and destination left=21, y=-100..100 is completely outside the rectangle's ultimate bottom and top
    [InlineData("Move up-right by (20, 20); Vertical Segment contained in path", 20, 20, Axis2.Y, 5, 7.1, 7.2, true)] // x=5 is between starting right=4 and destination left=21, y=7.1..7.2 is above the starting top=7 and completely inside the path
    [InlineData("Move down-left by (-1, -2) putting rect at origin; Horizontal Segment intersects destination left", -1, -2, Axis2.X, 2, -0.1, 0.1, true)] // y=2 is between dest bottom=0 and top=5, crosses left of destination which is at x=0
    // TODO: touching
    // TODO: Degenerate segments (min == max) (include a case passing through origin)
    public void TrySweepIntersectionCases(string name,
                                          float rectDx,
                                          float rectDy,
                                          Axis2 segmentAxis,
                                          float segmentIntercept,
                                          float segmentMin,
                                          float segmentMax,
                                          bool expected)
    {
        var width = 3;
        var height = 5;
        var start = new AlignedRectangle(new(1, 2), width, height);
        var destination = new AlignedRectangle(new(1 + rectDx, 2 + rectDy), width, height);
        var segment = new AxisAlignedSegment2(segmentAxis, new(segmentIntercept, segmentIntercept), new(segmentMin, segmentMax));

        start.TrySweepIntersection(destination, segment, out _).Should().Be(expected, name);
        destination.TrySweepIntersection(start, segment, out _).Should().Be(expected, name + " (reversed)");
    }

    [Fact(DisplayName = "Swept rectangle rejects a destination with a different size")]
    public void TrySweepIntersection_DifferentSize_ThrowsArgumentException()
    {
        var start = new AlignedRectangle(new(0, 0), 2, 2);
        var destination = new AlignedRectangle(new(10, 10), 3, 2);
        var segment = new AxisAlignedSegment2(Axis2.X, new(0, 5), new(0, 10));

        var act = () => start.TrySweepIntersection(destination, segment, out _);

        act.Should().Throw<ArgumentException>().WithParameterName(nameof(destination));
    }

    [Fact(DisplayName = "Swept rectangle returns first contact information")]
    public void TrySweepIntersection_ReturnsFirstContact()
    {
        var start = new AlignedRectangle(new(1, 2), 3, 5);
        var destination = start with { BottomLeft = new Vector2(21, 22) };
        var segment = new AxisAlignedSegment2(Axis2.Y, new(14, 0), new(-100, 100));

        var intersects = start.TrySweepIntersection(destination, segment, out var hit);

        intersects.Should().BeTrue();
        hit.Time.Should().Be(0.5f);
        hit.ContactBottomLeft.Should().Be(new Vector2(11, 12));
        hit.Normal.Should().Be(-Vector2.UnitX);
    }

    [Fact(DisplayName = "Swept rectangle combines normals at a corner contact")]
    public void TrySweepIntersection_CornerContact_ReturnsDiagonalNormal()
    {
        var start = new AlignedRectangle(new(1, 2), 3, 5);
        var destination = start with { BottomLeft = new Vector2(21, 22) };
        var point = new AxisAlignedSegment2(Axis2.Y, new(14, 0), new(17, 17));

        var intersects = start.TrySweepIntersection(destination, point, out var hit);

        intersects.Should().BeTrue();
        hit.Time.Should().Be(0.5f);
        hit.ContactBottomLeft.Should().Be(new Vector2(11, 12));
        hit.Normal.Should().Be(Vector2.Normalize(new Vector2(-1, -1)));
    }

    [Fact(DisplayName = "Swept rectangle can move away from an adjacent segment")]
    public void TrySweepIntersection_MovingAwayFromContact_ReturnsFalse()
    {
        var start = new AlignedRectangle(new(1, 2), 3, 5);
        var destination = start with { BottomLeft = new Vector2(0, 2) };
        var segment = new AxisAlignedSegment2(Axis2.Y, new(4, 0), new(2, 7));

        start.TrySweepIntersection(destination, segment, out _).Should().BeFalse();
    }

    [Fact(DisplayName = "Sweep hit shortens movement to requested clearance")]
    public void SweepHit_GetAllowedMovement_AppliesClearance()
    {
        var hit = new SweepHit(0.5f, new Vector2(11, 7), -Vector2.UnitX);
        var requestedMovement = new Vector2(20, 10);

        hit.GetAllowedMovement(requestedMovement).Should().Be(new Vector2(10, 5));
        hit.GetAllowedMovement(requestedMovement, clearance: 1f)
            .Should().Be(new Vector2(9, 4.5f));
    }

    [Fact(DisplayName = "Sliding movement preserves tangent component after wall hit")]
    public void GetAllowedSlidingMovement_DiagonalIntoVerticalWall_SlidesUp()
    {
        var start = new AlignedRectangle(new(1, 1), 2, 2);
        var requested = new Vector2(20, 20);
        var wall = new AxisAlignedSegment2(Axis2.Y, new(10, 0), new(-10, 50));

        var allowed = start.GetAllowedSlidingMovement(
            requested,
            [wall],
            clearance: 0.001f);

        var resolved = start.BottomLeft + allowed;
        resolved.X.Should().BeApproximately(10f - 2f - 0.001f, 0.00001f);
        resolved.Y.Should().BeApproximately(21f, 0.00001f);
    }

    [Fact(DisplayName = "Sliding movement rechecks collision and stops at second wall")]
    public void GetAllowedSlidingMovement_SlideThenCeilingHit_StopsAtCeiling()
    {
        var start = new AlignedRectangle(new(1, 1), 2, 2);
        var requested = new Vector2(20, 20);
        var walls = new[]
        {
            new AxisAlignedSegment2(Axis2.Y, new(10, 0), new(-10, 50)),
            new AxisAlignedSegment2(Axis2.X, new(0, 14), new(-10, 50))
        };

        var allowed = start.GetAllowedSlidingMovement(
            requested,
            walls,
            clearance: 0.001f);

        var resolved = start.BottomLeft + allowed;
        resolved.X.Should().BeApproximately(10f - 2f - 0.001f, 0.00001f);
        resolved.Y.Should().BeApproximately(14f - 2f - 0.001f, 0.00001f);
    }
}
