using Wrecs.Geometry;

namespace Wrecs.Tests.Geometry;

public class CircleTests
{
    [Fact(DisplayName = "Circle bounds are centered and sized by radius")]
    public void Bounds_ReturnsEnclosingAlignedRectangle()
    {
        var circle = new Circle(new Vector2(2.5f, -1.5f), 3f);

        circle.Bounds.Should().Be(new AlignedRectangle(
            new Vector2(-0.5f, -4.5f),
            Width: 6f,
            Height: 6f));
    }
}
