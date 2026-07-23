using System.Diagnostics;

namespace Wrecs.Geometry;

[DebuggerDisplay("({Start.X}, {Start.Y}) -> ({End.X}, {End.Y})")]
public readonly struct LineSegment(Vector2 start, Vector2 end)
{
    public Vector2 Start { get; } = start;
    public Vector2 End { get; } = end;

    public bool Intersects(LineSegment other)
    {
        return SegmentUtilities.SegmentsIntersect(Start, End, other.Start, other.End);
    }
}
