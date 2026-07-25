using Wrecs.Geometry;

namespace Wrecs.Tests.Geometry;

public class AxisAlignedSegment2Tests
{
    [Fact(DisplayName = "X-aligned segment derives horizontal endpoints")]
    public void Constructor_XAxis_DerivesHorizontalEndpoints()
    {
        var segment = new AxisAlignedSegment2(
            Axis2.X,
            new Vector2(99, 5),
            new Interval(2, 10));

        segment.Axis.Should().Be(Axis2.X);
        segment.Anchor.Should().Be(new Vector2(0, 5));
        segment.Start.Should().Be(new Vector2(2, 5));
        segment.End.Should().Be(new Vector2(10, 5));
        segment.Length.Should().Be(8);
    }

    [Fact(DisplayName = "Y-aligned segment derives vertical endpoints")]
    public void Constructor_YAxis_DerivesVerticalEndpoints()
    {
        var segment = new AxisAlignedSegment2(
            Axis2.Y,
            new Vector2(3, 99),
            new Interval(-4, 6));

        segment.Axis.Should().Be(Axis2.Y);
        segment.Anchor.Should().Be(new Vector2(3, 0));
        segment.Start.Should().Be(new Vector2(3, -4));
        segment.End.Should().Be(new Vector2(3, 6));
        segment.Length.Should().Be(10);
    }

    [Theory(DisplayName = "X-aligned segment contains points on its closed extent")]
    [InlineData(2, 5, true)]
    [InlineData(6, 5, true)]
    [InlineData(10, 5, true)]
    [InlineData(1, 5, false)]
    [InlineData(11, 5, false)]
    [InlineData(6, 5.01, false)]
    public void Contains_XAxisPoint_ReturnsExpectedResult(float x, float y, bool expected)
    {
        var segment = new AxisAlignedSegment2(
            Axis2.X,
            new Vector2(0, 5),
            new Interval(2, 10));

        segment.Contains(new Vector2(x, y)).Should().Be(expected);
    }

    [Theory(DisplayName = "Y-aligned segment contains points on its closed extent")]
    [InlineData(3, -4, true)]
    [InlineData(3, 1, true)]
    [InlineData(3, 6, true)]
    [InlineData(3, -5, false)]
    [InlineData(3, 7, false)]
    [InlineData(3.01f, 1, false)]
    public void Contains_YAxisPoint_ReturnsExpectedResult(float x, float y, bool expected)
    {
        var segment = new AxisAlignedSegment2(
            Axis2.Y,
            new Vector2(3, 0),
            new Interval(-4, 6));

        segment.Contains(new Vector2(x, y)).Should().Be(expected);
    }

    [Fact(DisplayName = "Segment equality ignores the anchor coordinate along its axis")]
    public void Equality_DifferentUnusedAnchorCoordinates_IsEqual()
    {
        var first = new AxisAlignedSegment2(
            Axis2.X,
            new Vector2(-100, 5),
            new Interval(2, 10));
        var second = new AxisAlignedSegment2(
            Axis2.X,
            new Vector2(100, 5),
            new Interval(2, 10));

        first.Should().Be(second);
    }

    [Fact(DisplayName = "Segments with different fixed coordinates are not equal")]
    public void Equality_DifferentFixedCoordinates_IsNotEqual()
    {
        var first = new AxisAlignedSegment2(
            Axis2.X,
            new Vector2(0, 5),
            new Interval(2, 10));
        var second = new AxisAlignedSegment2(
            Axis2.X,
            new Vector2(0, 6),
            new Interval(2, 10));

        first.Should().NotBe(second);
    }

    [Fact(DisplayName = "Segments aligned to different axes are not equal")]
    public void Equality_DifferentAxes_IsNotEqual()
    {
        var horizontal = new AxisAlignedSegment2(
            Axis2.X,
            Vector2.Zero,
            new Interval(2, 10));
        var vertical = new AxisAlignedSegment2(
            Axis2.Y,
            Vector2.Zero,
            new Interval(2, 10));

        horizontal.Should().NotBe(vertical);
    }

    [Fact(DisplayName = "Segment rejects an unsupported axis")]
    public void Constructor_UnsupportedAxis_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new AxisAlignedSegment2(
            (Axis2)99,
            Vector2.Zero,
            new Interval(0, 1));

        act.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithParameterName("axis");
    }
}
