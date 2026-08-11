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
    public readonly Interval HorizontalRange => new(Left, Right);
    public readonly Interval VerticalRange => new(Bottom, Top);

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

    public readonly IntersectionRelation GetIntersectionRelation(AlignedRectangle other)
    {
        if (Left > other.Right ||
            Right < other.Left ||
            Bottom > other.Top ||
            Top < other.Bottom)
        {
            return IntersectionRelation.Disjoint;
        }

        return Left < other.Right &&
               Right > other.Left &&
               Bottom < other.Top &&
               Top > other.Bottom
            ? IntersectionRelation.Overlapping
            : IntersectionRelation.Touching;
    }

    public readonly bool Overlaps(AlignedRectangle other) =>
        GetIntersectionRelation(other) == IntersectionRelation.Overlapping;

    public readonly bool Touches(AlignedRectangle other) =>
        GetIntersectionRelation(other) == IntersectionRelation.Touching;

    public readonly bool OverlapsOrTouches(AlignedRectangle other) =>
        GetIntersectionRelation(other) != IntersectionRelation.Disjoint;

    public readonly IntersectionRelation GetIntersectionRelation(AxisAlignedSegment2 segment)
    {
        var overlapsOrTouches = segment.Axis switch
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

        if (!overlapsOrTouches)
            return IntersectionRelation.Disjoint;

        var overlaps = segment.Axis switch
        {
            Axis2.X =>
                segment.Anchor.Y > Bottom &&
                segment.Anchor.Y < Top &&
                segment.Interval.Max > Left &&
                segment.Interval.Min < Right,
            Axis2.Y =>
                segment.Anchor.X > Left &&
                segment.Anchor.X < Right &&
                segment.Interval.Max > Bottom &&
                segment.Interval.Min < Top,
            _ => false
        };

        return overlaps
            ? IntersectionRelation.Overlapping
            : IntersectionRelation.Touching;
    }

    public readonly bool Overlaps(AxisAlignedSegment2 segment) =>
        GetIntersectionRelation(segment) == IntersectionRelation.Overlapping;

    public readonly bool Touches(AxisAlignedSegment2 segment) =>
        GetIntersectionRelation(segment) == IntersectionRelation.Touching;

    public readonly bool OverlapsOrTouches(AxisAlignedSegment2 segment) =>
        GetIntersectionRelation(segment) != IntersectionRelation.Disjoint;

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
    /// Finds the first point at which this rectangle touches or crosses an
    /// axis-aligned segment while moving in a straight line to
    /// <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">
    /// The rectangle's final position. It must have the same width and height as
    /// this rectangle because this operation supports translation only, not resizing
    /// or rotation.
    /// </param>
    /// <param name="segment">
    /// The stationary horizontal or vertical segment to test.
    /// </param>
    /// <param name="hit">
    /// When this method returns <see langword="true"/>, contains the first contact's
    /// normalized time, rectangle position, and outward-facing normal.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the rectangle intersects or touches the segment
    /// during the movement; otherwise, <see langword="false"/>.
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
    ///
    /// <para>
    /// Contact at the starting position is ignored when the movement immediately
    /// separates the rectangle from the segment.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// <paramref name="destination"/> does not have the same dimensions as this
    /// rectangle.
    /// </exception>
    public readonly bool TrySweepIntersection(AlignedRectangle destination,
                                              AxisAlignedSegment2 segment,
                                              out SweepHit hit)
    {
        if (Width != destination.Width || Height != destination.Height)
        {
            throw new ArgumentException(
                "Destination must have the same width and height as the source rectangle.",
                nameof(destination));
        }

        // Treat the moving rectangle as its bottom-left corner. Expand the
        // stationary segment into every corner position that would cause the
        // rectangle to touch it.
        //
        // Because the rectangle extends rightward by Width and upward by Height
        // from its bottom-left corner, the segment's bounds are expanded leftward
        // and downward by those amounts.
        var obstacle = segment.Bounds.Dilate(left: Width,
                                             right: 0f,
                                             bottom: Height,
                                             top: 0f);

        // Describe the bottom-left corner's path as:
        //
        //     position(t) = BottomLeft + movement * t
        //
        // where t ranges from 0 at the current position to 1 at the destination.
        var movement = destination.BottomLeft - BottomLeft;

        var startedInside =
            BottomLeft.X > obstacle.Left &&
            BottomLeft.X < obstacle.Right &&
            BottomLeft.Y > obstacle.Bottom &&
            BottomLeft.Y < obstacle.Top;
        var startedOnBoundary = !startedInside && obstacle.Contains(BottomLeft);

        var startNormal = startedOnBoundary
            ? GetBoundaryNormal(obstacle, BottomLeft)
            : Vector2.Zero;
        var featureScale = MathF.Max(Width, Height);
        var distanceTolerance = GeometryTolerance.GetDistance(BottomLeft,
                                                              obstacle,
                                                              featureScale);
        var directionTolerance = GeometryTolerance.GetDirection(distanceTolerance,
                                                                movement,
                                                                featureScale);
        var initialContact = SweepMath.ClassifyInitialContact(startedInside,
                                                             startedOnBoundary,
                                                             movement,
                                                             startNormal,
                                                             directionTolerance);

        if (initialContact.IsResolved)
        {
            hit = initialContact.Hit;
            return initialContact.HasHit;
        }

        var timeTolerance = GeometryTolerance.GetTime(distanceTolerance, movement);
        if (!SweepMath.TryGetRayBoundsHit(BottomLeft,
                                          movement,
                                          obstacle,
                                          timeTolerance,
                                          out hit,
                                          out var exitTime))
        {
            return false;
        }

        // Ignore contact that exists only at t = 0 while moving away. This lets
        // a rectangle that starts adjacent to a wall move away from it.
        if (!startedInside && exitTime <= 0f)
        {
            hit = default;
            return false;
        }

        return true;
    }

    private static Vector2 GetBoundaryNormal(AlignedRectangle obstacle, Vector2 point)
    {
        var normal = Vector2.Zero;

        if (point.X == obstacle.Left)
            normal -= Vector2.UnitX;
        if (point.X == obstacle.Right)
            normal += Vector2.UnitX;
        if (point.Y == obstacle.Bottom)
            normal -= Vector2.UnitY;
        if (point.Y == obstacle.Top)
            normal += Vector2.UnitY;

        return normal == Vector2.Zero ? Vector2.Zero : Vector2.Normalize(normal);
    }

    /// <summary>
    /// Resolves movement against axis-aligned segments by iteratively sweeping,
    /// stopping at contact, and preserving only the unblocked tangent component.
    /// </summary>
    /// <param name="requestedMovement">The desired movement from this rectangle.</param>
    /// <param name="segments">The static wall segments to collide with.</param>
    /// <param name="clearance">
    /// The minimum distance to leave from each contacted wall.
    /// </param>
    /// <param name="maxIterations">
    /// Maximum number of collision iterations performed for one movement.
    /// </param>
    /// <param name="minimumMovement">
    /// Movements shorter than this are treated as zero to prevent jitter.
    /// </param>
    /// <returns>
    /// The largest safe movement found after iterative collision and slide
    /// resolution.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maxIterations"/> is less than 1.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="minimumMovement"/> is less than 0.
    /// </exception>
    public readonly Vector2 GetAllowedSlidingMovement(Vector2 requestedMovement,
                                                      IEnumerable<AxisAlignedSegment2> segments,
                                                      float clearance = 0f,
                                                      int maxIterations = 6,
                                                      float minimumMovement = 0.00001f)
    {
        return SweptMovement.GetAllowedSlidingMovement(
            this,
            requestedMovement,
            segments,
            static (rectangle, movement) => rectangle with
            {
                BottomLeft = rectangle.BottomLeft + movement
            },
            static (AlignedRectangle source,
                    AlignedRectangle destination,
                    AxisAlignedSegment2 segment,
                    out SweepHit hit) =>
                source.TrySweepIntersection(destination, segment, out hit),
            clearance,
            maxIterations,
            minimumMovement);
    }

    public readonly AlignedRectangle Dilate(float padding)
    {
        return Dilate(padding, padding, padding, padding);
    }

    public readonly AlignedRectangle Dilate(float left, float bottom, float right, float top)
    {
        return FromLBRT(Left - left, Bottom - bottom, Right + right, Top + top);
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

    public static AlignedRectangle FromMinMax(Vector2 min, Vector2 max)
    {
        return new AlignedRectangle(min, max.X - min.X, max.Y - min.Y);
    }
}
