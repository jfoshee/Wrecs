using System.Diagnostics;

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
        var useX = MathF.Max(MathF.Abs(direction.X), MathF.Abs(otherDirection.X)) >=
                   MathF.Max(MathF.Abs(direction.Y), MathF.Abs(otherDirection.Y));
        var start = useX ? Start.X : Start.Y;
        var end = useX ? End.X : End.Y;
        var otherStart = useX ? other.Start.X : other.Start.Y;
        var otherEnd = useX ? other.End.X : other.End.Y;
        var overlap = MathF.Min(MathF.Max(start, end), MathF.Max(otherStart, otherEnd)) -
                      MathF.Max(MathF.Min(start, end), MathF.Min(otherStart, otherEnd));

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
}
