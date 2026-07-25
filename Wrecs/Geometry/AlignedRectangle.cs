namespace Wrecs.Geometry;

/// <summary>
/// Axis-aligned rectangle.
/// Assumes the Y-axis is pointing up.
/// </summary>
public record struct AlignedRectangle(Vector2 BottomLeft, float Width, float Height)
{
    public AlignedRectangle(Vector2 bottomLeft, float size)
        : this(bottomLeft, size, size)
    {
    }

    public AlignedRectangle(Vector2 bottomLeft, Vector2 size)
        : this(bottomLeft, size.X, size.Y)
    {
    }

    public readonly float Left => BottomLeft.X;
    public readonly float Right => BottomLeft.X + Width;
    public readonly float Bottom => BottomLeft.Y;
    public readonly float Top => BottomLeft.Y + Height;
    public readonly Vector2 Size => new(Width, Height);
    public readonly Vector2 Center => BottomLeft + new Vector2(Width / 2, Height / 2);
    public readonly Vector2[] Corners => [
        BottomLeft,
        new Vector2(Right, Bottom),
        new Vector2(Right, Top),
        new Vector2(Left, Top)
    ];

    public readonly bool Contains(Vector2 point)
    {
        return point.X >= Left && point.X <= Right &&
               point.Y >= Bottom && point.Y <= Top;
    }

    public readonly bool Intersects(AlignedRectangle other)
    {
        return Left < other.Right &&
               Right > other.Left &&
               Bottom < other.Top &&
               Top > other.Bottom;
    }

    public readonly bool Intersects(AxisAlignedSegment2 segment)
    {
        return segment.Axis switch
        {
            Axis2.X =>
                segment.Anchor.Y >= Bottom &&
                segment.Anchor.Y <= Top &&
                segment.Interval.Max >= Left &&
                segment.Interval.Min <= Right,
            Axis2.Y =>
                segment.Anchor.X >= Left &&
                segment.Anchor.X <= Right &&
                segment.Interval.Max >= Bottom &&
                segment.Interval.Min <= Top,
            _ => false
        };
    }

    /// <summary>
    /// Determines the relative position of the specified point with respect to this rectangle.
    /// </summary>
    /// <param name="point">The point whose relative position is to be determined.</param>
    /// <returns>
    /// A <see cref="RelativePosition"/> bitfield indicating which relative position(s)
    /// the point occupies in relation to this rectangle.
    /// </returns>
    public readonly RelativePosition GetRelativePosition(Vector2 point)
    {
        if (point.X >= Left && point.X <= Right && point.Y >= Bottom && point.Y <= Top)
        {
            return RelativePosition.Inside;
        }

        var position = RelativePosition.None;

        if (point.Y > Top)
            position |= RelativePosition.Above;
        if (point.Y < Bottom)
            position |= RelativePosition.Below;
        if (point.X < Left)
            position |= RelativePosition.Left;
        if (point.X > Right)
            position |= RelativePosition.Right;

        return position;
    }

    public readonly AlignedRectangle Union(AlignedRectangle alignedRectangle)
    {
        if (this == Empty)
            return alignedRectangle;
        if (alignedRectangle == Empty)
            return this;
        var left = Math.Min(Left, alignedRectangle.Left);
        var right = Math.Max(Right, alignedRectangle.Right);
        var bottom = Math.Min(Bottom, alignedRectangle.Bottom);
        var top = Math.Max(Top, alignedRectangle.Top);
        return FromLBRT(left, bottom, right, top);
    }

    public readonly AlignedRectangle Dilate(float padding)
    {
        return FromLBRT(Left - padding, Bottom - padding, Right + padding, Top + padding);
    }

    public readonly AlignedRectangle Scale(Vector2 scale)
    {
        return new(BottomLeft * scale, Width * scale.X, Height * scale.Y);
    }

    public static AlignedRectangle FromLBRT(float left, float bottom, float right, float top)
    {
        return new(new Vector2(left, bottom), right - left, top - bottom);
    }

    public static AlignedRectangle Empty { get; } = new(Vector2.Zero, 0, 0);
    public static AlignedRectangle UnitSquare { get; } = new(Vector2.Zero, 1, 1);

    /// <summary>
    /// Creates a new AlignedRectangle centered at the given position with the specified size.
    /// </summary>
    /// <param name="center">The center point of the rectangle.</param>
    /// <param name="size">The size of the rectangle as a Vector2.</param>
    /// <returns>An AlignedRectangle centered at the specified position.</returns>
    public static AlignedRectangle Centered(Vector2 center, Vector2 size)
    {
        // Calculate the bottom-left corner based on the center and size
        var bottomLeft = center - new Vector2(size.X / 2, size.Y / 2);
        return new AlignedRectangle(bottomLeft, size);
    }

    /// <summary>
    /// Creates a new AlignedRectangle centered at the given position with a square size.
    /// </summary>
    /// <param name="center">The center point of the rectangle.</param>
    /// <param name="size">The size of the square rectangle.</param>
    /// <returns>An AlignedRectangle centered at the specified position.</returns>
    public static AlignedRectangle Centered(Vector2 center, float size)
    {
        return Centered(center, new Vector2(size, size));
    }

    /// <summary>
    /// Creates a new AlignedRectangle centered at the given position with the specified size.
    /// </summary>
    public static AlignedRectangle Centered(Vector2 center, float width, float height)
    {
        return Centered(center, new Vector2(width, height));
    }

    public static AlignedRectangle FromPoints(params Vector2[] corners)
    {
        var left = corners.Min(corner => corner.X);
        var right = corners.Max(corner => corner.X);
        var bottom = corners.Min(corner => corner.Y);
        var top = corners.Max(corner => corner.Y);
        return FromLBRT(left, bottom, right, top);
    }
}
