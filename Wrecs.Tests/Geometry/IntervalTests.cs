namespace Wrecs.Tests.Geometry;

public class IntervalTests
{
    [Fact(DisplayName = "Interval exposes its bounds and length")]
    public void Properties_ValidBounds_ReturnExpectedValues()
    {
        var interval = new Wrecs.Geometry.Interval(2, 10);

        interval.Min.Should().Be(2);
        interval.Max.Should().Be(10);
        interval.Length.Should().Be(8);
    }

    [Theory(DisplayName = "Interval contains values within its closed bounds")]
    [InlineData(2, true)]
    [InlineData(6, true)]
    [InlineData(10, true)]
    [InlineData(1.99, false)]
    [InlineData(10.01, false)]
    public void Contains_Value_ReturnsExpectedResult(float value, bool expected)
    {
        var interval = new Wrecs.Geometry.Interval(2, 10);

        interval.Contains(value).Should().Be(expected);
    }

    [Fact(DisplayName = "Interval permits equal bounds")]
    public void Constructor_EqualBounds_CreatesZeroLengthInterval()
    {
        var interval = new Wrecs.Geometry.Interval(3, 3);

        interval.Length.Should().Be(0);
        interval.Contains(3).Should().BeTrue();
    }

    [Fact(DisplayName = "Interval rejects reversed bounds")]
    public void Constructor_ReversedBounds_ThrowsArgumentException()
    {
        var act = () => new Wrecs.Geometry.Interval(10, 2);

        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("max");
    }
}
