using Wrecs.Geometry;
using static Wrecs.Geometry.CoordinateConversion;

namespace Wrecs.Tests.Geometry;

public class CoordinateConversionTest
{
    [Theory(DisplayName = "Rectangle Conversion")]
    [InlineData("1x1 in top left", 0, 0, 1, 1, 0, 100, 100, 0, 99, 0.5f, 99.5f)]
    [InlineData("1x1 in top right", 99, 0, 1, 1, 0, 100, 100, 99, 99, 99.5f, 99.5f)]
    // See CoordinateConversion1.png:
    [InlineData("1x1 in top left 45°", 0.5f, -0.21f, 1, 1, 45.0f, 10, 10, -0.21f, 9.5f, 0.5f, 9.5f)]
    // See CoordinateConversion2.png:
    [InlineData("3x2 rotated -30°", 1, 2, 3, 2, -30, 7, 7, 2.0f, 3.27f, 2.8f, 4.88f)]
    // See CoordinateConversion3.png for Tile Object anchored on bottom left:
    [InlineData("1x1 anchored bottom left 15°", 1, 1, 1, 1, 15, 2, 2, 1, 1, 1.61f, 1.35f, AnchorPoint.BottomLeft)]
    public void RectConversion(string name,
                               float x,
                               float y,
                               float rectWidth,
                               float rectHeight,
                               float rotationDegrees,
                               float boundsWidth,
                               float boundsHeight,
                               float bottomLeftX,
                               float bottomLeftY,
                               float centerX,
                               float centerY,
                               AnchorPoint anchorPoint = AnchorPoint.TopLeft)
    {
        var rotationRadians = Angle.ToRadians(rotationDegrees);
        var boundsSize = new Vector2(boundsWidth, boundsHeight);

        var rotatedRect = RectangleFromScreenCoordinates(x,
                                                         y,
                                                         rectWidth,
                                                         rectHeight,
                                                         rotationRadians,
                                                         boundsSize,
                                                         tileSize: Vector2.One,
                                                         anchorPoint);

        rotatedRect.RotationRadians.Should().Be(-rotationRadians);
        rotatedRect.Center.Round(2).Should().Be(new Vector2(centerX, centerY), name);
        rotatedRect.Corners[0].Round(2).Should().Be(new Vector2(bottomLeftX, bottomLeftY), name);
        rotatedRect.OriginalAlignedRectangle.Width.Should().Be(rectWidth);
        rotatedRect.OriginalAlignedRectangle.Height.Should().Be(rectHeight);
    }
}
