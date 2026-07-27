using System.Runtime.CompilerServices;

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

    /// <summary>
    /// Returns true if vertically or horizontally aligned with the other rectangle.
    /// </summary>
    public readonly bool IsAlignedWith(AlignedRectangle other)
    {
        return (BottomLeft.X == other.BottomLeft.X && Width == other.Width) ||
               (BottomLeft.Y == other.BottomLeft.Y && Height == other.Height);
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

    /// <summary>
    /// Returns the axis-aligned rectangle swept out by translating this rectangle
    /// horizontally or vertically to <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">
    /// A rectangle of the same size whose bottom-left corner shares an X or Y
    /// coordinate with this rectangle.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination"/> is not a horizontal or vertical
    /// translation of this rectangle.
    /// </exception>
    public readonly AlignedRectangle Sweep(AlignedRectangle destination)
    {
        var isSameSize = Width == destination.Width && Height == destination.Height;
        var isAxisAlignedTranslation = IsAlignedWith(destination);

        if (!isSameSize || !isAxisAlignedTranslation)
        {
            throw new ArgumentException(
                "Destination must be a horizontal or vertical translation of the same rectangle.",
                nameof(destination));
        }

        var left = Math.Min(Left, destination.Left);
        var right = Math.Max(Right, destination.Right);
        var bottom = Math.Min(Bottom, destination.Bottom);
        var top = Math.Max(Top, destination.Top);
        return FromLBRT(left, bottom, right, top);
    }
    /// <summary>
    /// Determines whether this rectangle touches or crosses an axis-aligned segment
    /// while moving in a straight line to <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">
    /// The rectangle's final position. It must have the same width and height as
    /// this rectangle because this operation supports translation only, not resizing
    /// or rotation.
    /// </param>
    /// <param name="segment">
    /// The stationary horizontal or vertical segment to test.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the rectangle intersects or touches the segment at
    /// any point during the movement, including at its starting or ending position;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Testing every intermediate position of the moving rectangle would be
    /// inefficient. Instead, this method tracks only the rectangle's bottom-left
    /// corner.
    /// </para>
    ///
    /// <para>
    /// To make that possible, the stationary segment is expanded into a rectangle
    /// containing every bottom-left-corner position that would cause the moving
    /// rectangle to touch the original segment.
    /// </para>
    ///
    /// <para>
    /// For example, suppose a rectangle of width <c>W</c> and height <c>H</c>
    /// approaches a horizontal segment. Its bottom-left corner may be as much as
    /// <c>W</c> units to the left of the segment and still overlap it. Similarly,
    /// the corner may be as much as <c>H</c> units below the segment and still
    /// reach it. The horizontal segment can therefore be replaced by this expanded
    /// rectangle:
    /// </para>
    ///
    /// <code>
    /// [segment.MinX - W, segment.MaxX]
    ///     ×
    /// [segment.Y - H, segment.Y]
    /// </code>
    ///
    /// <para>
    /// The original problem is then equivalent to asking whether the bottom-left
    /// corner's straight-line path enters that expanded rectangle. This geometric
    /// transformation is formally related to a Minkowski sum, but no polygon or
    /// swept hexagon needs to be constructed.
    /// </para>
    ///
    /// <para>
    /// The path is represented using a normalized time value <c>t</c>. At
    /// <c>t = 0</c>, the rectangle is at its current position. At <c>t = 1</c>,
    /// it is at <paramref name="destination"/>. The helper method determines the
    /// range of times during which the moving point is inside each coordinate
    /// range of the expanded obstacle. A collision occurs when the valid X and Y
    /// time ranges overlap.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// <paramref name="destination"/> does not have the same dimensions as this
    /// rectangle.
    /// </exception>
    public readonly bool SweepIntersects(AlignedRectangle destination, AxisAlignedSegment2 segment)
    {
        if (Width != destination.Width || Height != destination.Height)
        {
            throw new ArgumentException(
                "Destination must have the same width and height as the source rectangle.",
                nameof(destination));
        }

        float obstacleLeft;
        float obstacleRight;
        float obstacleBottom;
        float obstacleTop;

        // Replace the stationary segment with the set of all bottom-left-corner
        // positions that would make this rectangle touch that segment.
        //
        // Once this expanded obstacle has been constructed, the moving rectangle
        // can be treated as a single moving point: its bottom-left corner.
        switch (segment.Axis)
        {
            case Axis2.X:
                // For a horizontal segment:
                //
                // The moving rectangle can reach Width units to the right of its
                // bottom-left corner, so valid corner positions extend Width units
                // to the left of the segment.
                //
                // It can also reach Height units above its bottom-left corner, so
                // valid corner positions extend Height units below the segment.
                obstacleLeft = segment.Interval.Min - Width;
                obstacleRight = segment.Interval.Max;
                obstacleBottom = segment.Anchor.Y - Height;
                obstacleTop = segment.Anchor.Y;
                break;

            case Axis2.Y:
                // For a vertical segment:
                //
                // Valid bottom-left-corner positions extend Width units left of
                // the segment and Height units below its lower endpoint.
                obstacleLeft = segment.Anchor.X - Width;
                obstacleRight = segment.Anchor.X;
                obstacleBottom = segment.Interval.Min - Height;
                obstacleTop = segment.Interval.Max;
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(segment),
                    segment.Axis,
                    "The segment must be aligned with either the X or Y axis.");
        }

        // Describe the bottom-left corner's path as:
        //
        //     position(t) = BottomLeft + movement * t
        //
        // where t ranges from 0 at the current position to 1 at the destination.
        var movement = destination.BottomLeft - BottomLeft;

        // These values describe the portion of the movement during which the point
        // could still be inside the expanded obstacle.
        //
        // Initially, the entire movement from t = 0 through t = 1 is possible.
        // Each axis test narrows this range.
        var entryTime = 0f;
        var exitTime = 1f;

        if (!RestrictTimeRangeToAxis(
                BottomLeft.X,
                movement.X,
                obstacleLeft,
                obstacleRight,
                ref entryTime,
                ref exitTime))
        {
            return false;
        }

        return RestrictTimeRangeToAxis(
            BottomLeft.Y,
            movement.Y,
            obstacleBottom,
            obstacleTop,
            ref entryTime,
            ref exitTime);
    }

    /// <summary>
    /// Restricts a movement's current valid time range to the times during which
    /// one coordinate lies between <paramref name="rangeMin"/> and
    /// <paramref name="rangeMax"/>.
    /// </summary>
    /// <param name="origin">
    /// The coordinate at the beginning of the movement.
    /// </param>
    /// <param name="movement">
    /// The coordinate's total change between normalized times 0 and 1.
    /// </param>
    /// <param name="rangeMin">
    /// The inclusive lower boundary of the obstacle on this axis.
    /// </param>
    /// <param name="rangeMax">
    /// The inclusive upper boundary of the obstacle on this axis.
    /// </param>
    /// <param name="entryTime">
    /// On input, the earliest time still valid after testing previous axes. On
    /// output, the later of that value and this axis's entry time.
    /// </param>
    /// <param name="exitTime">
    /// On input, the latest time still valid after testing previous axes. On
    /// output, the earlier of that value and this axis's exit time.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if some time remains valid after considering this
    /// axis; otherwise, <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool RestrictTimeRangeToAxis(float origin,
                                                float movement,
                                                float rangeMin,
                                                float rangeMax,
                                                ref float entryTime,
                                                ref float exitTime)
    {
        // This coordinate does not change during the movement. It can satisfy this
        // axis for the entire movement only when it is already within the obstacle's
        // inclusive coordinate range.
        if (movement == 0f)
            return origin >= rangeMin && origin <= rangeMax;

        // Find when the moving coordinate crosses each boundary:
        //
        //     origin + movement * t = boundary
        //
        // Solving for t gives:
        //
        //     t = (boundary - origin) / movement
        //
        // When movement is negative, the maximum boundary is encountered before
        // the minimum boundary, so the two times are reordered below.
        var inverseMovement = 1f / movement;
        var axisEntryTime = (rangeMin - origin) * inverseMovement;
        var axisExitTime = (rangeMax - origin) * inverseMovement;

        if (axisEntryTime > axisExitTime)
            (axisEntryTime, axisExitTime) = (axisExitTime, axisEntryTime);

        // A collision requires the X and Y coordinates to be inside their
        // respective ranges at the same time. Intersect this axis's valid time
        // interval with the interval retained from the previously tested axes.
        entryTime = MathF.Max(entryTime, axisEntryTime);
        exitTime = MathF.Min(exitTime, axisExitTime);

        // Equality represents touching the obstacle at exactly one instant and
        // therefore counts as an intersection.
        return entryTime <= exitTime;
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
