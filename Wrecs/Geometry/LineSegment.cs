using System.Diagnostics;
using static System.MathF;

namespace Wrecs.Geometry;

[DebuggerDisplay("({Start.X}, {Start.Y}) -> ({End.X}, {End.Y})")]
public readonly struct LineSegment(Vector2 start, Vector2 end)
{
    private enum PointSide
    {
        OnLine,
        Left,
        Right
    }

    public Vector2 Start { get; } = start;
    public Vector2 End { get; } = end;

    /// <summary>
    /// Classifies whether two finite segments are disjoint, meet at isolated
    /// points, or share a positive-length collinear interval.
    /// </summary>
    public IntersectionRelation GetIntersectionRelation(LineSegment other)
    {
        Span<Vector2> points = stackalloc Vector2[4];
        points[0] = Start;
        points[1] = End;
        points[2] = other.Start;
        points[3] = other.End;
        var distanceTolerance = GeometryTolerance.GetDistance(points);
        var distanceToleranceSquared = distanceTolerance * distanceTolerance;

        var direction = End - Start;
        var otherDirection = other.End - other.Start;
        var isPoint = direction.LengthSquared() <= distanceToleranceSquared;
        var otherIsPoint = otherDirection.LengthSquared() <=
                           distanceToleranceSquared;

        if (isPoint && otherIsPoint)
        {
            return Vector2.DistanceSquared(Start, other.Start) <=
                   distanceToleranceSquared
                ? IntersectionRelation.Touching
                : IntersectionRelation.Disjoint;
        }

        if (isPoint)
            return GetPointRelation(Start, other, distanceTolerance);
        if (otherIsPoint)
            return GetPointRelation(other.Start, this, distanceTolerance);

        var otherStartSide = GetPointSide(Start,
                                          End,
                                          other.Start,
                                          distanceTolerance);
        var otherEndSide = GetPointSide(Start,
                                        End,
                                        other.End,
                                        distanceTolerance);
        var startSide = GetPointSide(other.Start,
                                     other.End,
                                     Start,
                                     distanceTolerance);
        var endSide = GetPointSide(other.Start,
                                   other.End,
                                   End,
                                   distanceTolerance);
        var collinear = otherStartSide == PointSide.OnLine &&
                        otherEndSide == PointSide.OnLine &&
                        startSide == PointSide.OnLine &&
                        endSide == PointSide.OnLine;

        if (collinear)
            return GetCollinearRelation(other, distanceTolerance);

        if ((otherStartSide == PointSide.OnLine &&
             IsWithinBounds(other.Start, this, distanceTolerance)) ||
            (otherEndSide == PointSide.OnLine &&
             IsWithinBounds(other.End, this, distanceTolerance)) ||
            (startSide == PointSide.OnLine &&
             IsWithinBounds(Start, other, distanceTolerance)) ||
            (endSide == PointSide.OnLine &&
             IsWithinBounds(End, other, distanceTolerance)))
        {
            return IntersectionRelation.Touching;
        }

        return AreOpposite(otherStartSide, otherEndSide) &&
               AreOpposite(startSide, endSide)
            ? IntersectionRelation.Touching
            : IntersectionRelation.Disjoint;
    }

    private IntersectionRelation GetCollinearRelation(LineSegment other,
                                                       float distanceTolerance)
    {
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

        if (overlap > distanceTolerance)
            return IntersectionRelation.Overlapping;

        return overlap >= -distanceTolerance
            ? IntersectionRelation.Touching
            : IntersectionRelation.Disjoint;
    }

    private static IntersectionRelation GetPointRelation(Vector2 point,
                                                         LineSegment segment,
                                                         float distanceTolerance) =>
        GetPointSide(segment.Start,
                     segment.End,
                     point,
                     distanceTolerance) == PointSide.OnLine &&
        IsWithinBounds(point, segment, distanceTolerance)
            ? IntersectionRelation.Touching
            : IntersectionRelation.Disjoint;

    /// <summary>
    /// Classifies which side of a directed line contains a point. Points within
    /// the coordinate-aware distance margin are treated as lying on the line.
    /// </summary>
    private static PointSide GetPointSide(Vector2 lineStart,
                                          Vector2 lineEnd,
                                          Vector2 point,
                                          float distanceTolerance)
    {
        var direction = lineEnd - lineStart;
        var cross = Vector2.Cross(direction, point - lineStart);
        var crossTolerance = distanceTolerance * direction.Length();

        if (Abs(cross) <= crossTolerance)
            return PointSide.OnLine;

        return cross > 0f
            ? PointSide.Left
            : PointSide.Right;
    }

    private static bool IsWithinBounds(Vector2 point,
                                       LineSegment segment,
                                       float distanceTolerance) =>
        point.X >= Min(segment.Start.X, segment.End.X) - distanceTolerance &&
        point.X <= Max(segment.Start.X, segment.End.X) + distanceTolerance &&
        point.Y >= Min(segment.Start.Y, segment.End.Y) - distanceTolerance &&
        point.Y <= Max(segment.Start.Y, segment.End.Y) + distanceTolerance;

    private static bool AreOpposite(PointSide first, PointSide second) =>
        first == PointSide.Left && second == PointSide.Right ||
        first == PointSide.Right && second == PointSide.Left;

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
