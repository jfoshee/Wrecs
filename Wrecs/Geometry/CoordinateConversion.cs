namespace Wrecs.Geometry;

public static class CoordinateConversion
{
    /// <summary>
    /// Converts screen coordinates (origin at top left) to the code's coordinate system (origin at bottom left).
    /// </summary>
    /// <param name="boundsSize">The size of the bounding area</param>
    public static Vector2 PointFromScreenCoordinates(float x, float y, Vector2 boundsSize, Vector2 tileSize)
    {
        var boundsHeight = boundsSize.Y * tileSize.Y;
        return new Vector2(x, boundsHeight - y) / tileSize;
    }

    /// <summary>
    /// Converts screen coordinates of a rotated rectangle to the code's coordinate system.
    ///
    /// Screen Coordinates (aka Canvas Coordinates) are often used for images and UI elements.
    ///
    /// In screen coordinates:
    /// - The origin (0, 0) is at the top-left corner of the screen.
    /// - The rotation angle increases clockwise.
    ///
    /// In the code's Cartesian coordinate system:
    /// - The origin (0, 0) is at the bottom-left corner of the screen.
    /// - The rotation angle increases counter-clockwise.
    ///
    /// </summary>
    /// <param name="x">The x-coordinate of the rectangle in screen coordinates.</param>
    /// <param name="y">The y-coordinate of the rectangle in screen coordinates.</param>
    /// <param name="rectWidth">The width of the rectangle.</param>
    /// <param name="rectHeight">The height of the rectangle.</param>
    /// <param name="rotationRadians">The rotation angle of the rectangle in radians.</param>
    /// <param name="boundsSize">The size of the area in which the rectangle is located.</param>
    /// <param name="anchorPoint">Which corner of the rectangle is the given (x,y) and the center of rotation</param>
    /// <returns>A <see cref="RotatedRectangle"/> representing the rectangle in the code's coordinate system.</returns>
    public static RotatedRectangle RectangleFromScreenCoordinates(float x,
                                                                  float y,
                                                                  float rectWidth,
                                                                  float rectHeight,
                                                                  float rotationRadians,
                                                                  Vector2 boundsSize,
                                                                  Vector2 tileSize,
                                                                  AnchorPoint anchorPoint = AnchorPoint.TopLeft)
    {
        // Convert anchor position to code's coordinate system (Y origin at the bottom)
        var codeX = x;
        var boundsHeight = boundsSize.Y * tileSize.Y;
        var codeY = boundsHeight - y;

        // Adjust rotation angle (so angle increases counter-clockwise)
        float codeRotationRadians = -rotationRadians;

        // // Calculate the Anchor point in the code's coordinate system
        var A = new Vector2(codeX, codeY);

        // Calculate the center of the rectangle in the code's coordinate system
        var dh = anchorPoint == AnchorPoint.BottomLeft ? rectHeight / 2 : -rectHeight / 2;
        float centerX = codeX + rectWidth / 2;
        float centerY = codeY + dh;
        var C = new Vector2(centerX, centerY);

        // Vector from center to Anchor corner
        var V = A - C;

        // Rotate V by the rotation angle
        float cosTheta = MathF.Cos(codeRotationRadians);
        float sinTheta = MathF.Sin(codeRotationRadians);
        var V_rotated = new Vector2(
            V.X * cosTheta - V.Y * sinTheta,
            V.X * sinTheta + V.Y * cosTheta
        );

        // Adjusted center position
        var C_adj = A - V_rotated;

        // Calculate the adjusted bottom-left corner
        float rectBottomLeftX = C_adj.X - rectWidth / 2;
        float rectBottomLeftY = C_adj.Y - rectHeight / 2;
        var adjustedBottomLeft = new Vector2(rectBottomLeftX, rectBottomLeftY);

        // Create the AlignedRectangle and RotatedRectangle
        var alignedRectangle = new AlignedRectangle(adjustedBottomLeft, rectWidth, rectHeight);
        alignedRectangle = alignedRectangle.Scale(Vector2.One / tileSize);
        var rotatedRectangle = new RotatedRectangle(alignedRectangle, codeRotationRadians);

        return rotatedRectangle;
    }
}
