namespace Wrecs.Geometry;

/// <summary>
/// Represents a rectangle that can be rotated around its center.
/// </summary>
public readonly record struct RotatedRectangle
{
    public RotatedRectangle(AlignedRectangle alignedRectangle, float rotationRadians)
    {
        OriginalAlignedRectangle = alignedRectangle;
        RotationRadians = rotationRadians;
        Center = alignedRectangle.Center;
        var rotationMatrix = Matrix3x2.CreateRotation(rotationRadians, alignedRectangle.Center);
        InverseRotationMatrix = Matrix3x2.CreateRotation(-rotationRadians, alignedRectangle.Center);
        Corners = alignedRectangle.Corners.Select(corner => Vector2.Transform(corner, rotationMatrix)).ToArray();
    }

    /// <summary>
    /// The original aligned rectangle before rotation.
    /// </summary>
    public AlignedRectangle OriginalAlignedRectangle { get; }
    public float RotationRadians { get; }
    public Vector2 Center { get; }
    public Vector2[] Corners { get; }
    /// <summary>
    /// The axis-aligned bounding rectangle of the rotated rectangle.
    /// </summary>
    public AlignedRectangle BoundingRectangle => AlignedRectangle.FromPoints(Corners);
    private Matrix3x2 InverseRotationMatrix { get; }

    public readonly bool Contains(Vector2 point)
    {
        // Rotate the point into the rectangle's local space
        var transformedPoint = Vector2.Transform(point, InverseRotationMatrix);

        // Check if the transformed point is inside the aligned rectangle
        return OriginalAlignedRectangle.Contains(transformedPoint);
    }

    public readonly bool Intersects(AlignedRectangle other)
    {
        // Check if any of the rotated rectangle's corners are inside the aligned rectangle
        foreach (var corner in Corners)
        {
            if (other.Contains(corner))
            {
                return true;
            }
        }

        // Check if any of the aligned rectangle's corners are inside the rotated rectangle
        foreach (var otherCorner in other.Corners)
        {
            if (Contains(otherCorner))
            {
                return true;
            }
        }

        // Check if any edges intersect
        return SegmentUtilities.AnyEdgesIntersect(Corners, other.Corners);
    }

    public readonly RotatedRectangle Dilate(float radius)
    {
        var dilatedAlignedRect = OriginalAlignedRectangle.Dilate(radius);
        return new RotatedRectangle(dilatedAlignedRect, RotationRadians);
    }
}
