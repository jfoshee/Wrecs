using System.Diagnostics;
using static System.MathF;

namespace Wrecs.Geometry;

[DebuggerDisplay("({Start.X}, {Start.Y}) -> ({End.X}, {End.Y})")]
public readonly struct LineSegment(Vector2 start, Vector2 end)
{
    public Vector2 Start { get; } = start;
    public Vector2 End { get; } = end;

    public IntersectionRelation GetIntersectionRelation(LineSegment other)
    {
        if (!SegmentUtilities.SegmentsIntersect(Start, End, other.Start, other.End))
            return IntersectionRelation.Disjoint;

        var collinear =
            SegmentUtilities.Orientation(Start, End, other.Start) == 0f &&
            SegmentUtilities.Orientation(Start, End, other.End) == 0f;

        if (!collinear)
            return IntersectionRelation.Touching;

        var direction = End - Start;
        var otherDirection = other.End - other.Start;
        var useX = Max(Abs(direction.X), Abs(otherDirection.X)) >=
                   Max(Abs(direction.Y), Abs(otherDirection.Y));
        var start = useX ? Start.X : Start.Y;
        var end = useX ? End.X : End.Y;
        var otherStart = useX ? other.Start.X : other.Start.Y;
        var otherEnd = useX ? other.End.X : other.End.Y;
        var overlap = Min(Max(start, end), Max(otherStart, otherEnd)) -
                      Max(Min(start, end), Min(otherStart, otherEnd));

        return overlap > 0f
            ? IntersectionRelation.Overlapping
            : IntersectionRelation.Touching;
    }

    public bool Overlaps(LineSegment other) =>
        GetIntersectionRelation(other) == IntersectionRelation.Overlapping;

    public bool Touches(LineSegment other) =>
        GetIntersectionRelation(other) == IntersectionRelation.Touching;

    public bool OverlapsOrTouches(LineSegment other) =>
        GetIntersectionRelation(other) != IntersectionRelation.Disjoint;

    /// <summary>
    /// Returns the point on this finite segment nearest to
    /// <paramref name="point"/>.
    /// </summary>
    /// <remarks>
    /// The result may lie between the endpoints or on either endpoint. A
    /// zero-length segment returns its single endpoint.
    /// </remarks>
    public Vector2 GetClosestPoint(Vector2 point) =>
        GetClosestPoint(point, out _);

    /// <summary>
    /// Finds the closest point and its position along the segment, where zero is
    /// <see cref="Start"/> and one is <see cref="End"/>.
    /// </summary>
    internal Vector2 GetClosestPoint(Vector2 point, out float fraction)
    {
        var direction = End - Start;
        var lengthSquared = direction.LengthSquared();

        if (lengthSquared == 0f)
        {
            fraction = 0f;
            return Start;
        }

        var projectedFraction = Vector2.Dot(point - Start, direction) / lengthSquared;
        fraction = Max(0f, Min(projectedFraction, 1f));
        return Start + direction * fraction;
    }
}
