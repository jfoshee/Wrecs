using System.Diagnostics;

namespace Wrecs.Geometry;

/// <summary>
/// A closed, axis-aligned line segment in two-dimensional space.
/// </summary>
public readonly record struct AxisAlignedSegment2
{
    public AxisAlignedSegment2(Axis2 axis, Vector2 anchor, Interval interval)
    {
        if (!Enum.IsDefined(axis))
            throw new ArgumentOutOfRangeException(nameof(axis), axis, "Axis must be X or Y.");

        Axis = axis;
        Anchor = axis switch
        {
            Axis2.X => new Vector2(0, anchor.Y),
            Axis2.Y => new Vector2(anchor.X, 0),
            _ => throw new UnreachableException("Axis must be X or Y.")
        };
        Interval = interval;
    }

    public Axis2 Axis { get; }

    /// <summary>
    /// A point anchoring the segment's line. Its coordinate on <see cref="Axis"/> is normalized to zero.
    /// </summary>
    public Vector2 Anchor { get; }

    /// <summary>
    /// The segment's closed interval along <see cref="Axis"/>.
    /// </summary>
    public Interval Interval { get; }

    public float Length => Interval.Length;

    public Vector2 Start => PointAt(Interval.Min);
    public Vector2 End => PointAt(Interval.Max);

    public bool Contains(Vector2 point) =>
        Axis switch
        {
            Axis2.X => point.Y == Anchor.Y && Interval.Contains(point.X),
            Axis2.Y => point.X == Anchor.X && Interval.Contains(point.Y),
            _ => throw new UnreachableException("Axis must be X or Y.")
        };

    private Vector2 PointAt(float coordinate) =>
        Axis switch
        {
            Axis2.X => new Vector2(coordinate, Anchor.Y),
            Axis2.Y => new Vector2(Anchor.X, coordinate),
            _ => throw new UnreachableException("Axis must be X or Y.")
        };
}
