using static System.MathF;

namespace Wrecs.Geometry;

/// <summary>
/// Represents a rectangle that can be rotated around its center.
/// </summary>
public readonly record struct RotatedRectangle
{
    private readonly Vector2[] _corners;

    public RotatedRectangle(AlignedRectangle alignedRectangle, float rotationRadians)
    {
        OriginalAlignedRectangle = alignedRectangle;
        RotationRadians = rotationRadians;
        Center = alignedRectangle.Center;
        var rotationMatrix = Matrix3x2.CreateRotation(rotationRadians, alignedRectangle.Center);
        InverseRotationMatrix = Matrix3x2.CreateRotation(-rotationRadians, alignedRectangle.Center);
        _corners = alignedRectangle.Corners
            .Select(corner => Vector2.Transform(corner, rotationMatrix))
            .ToArray();
    }

    /// <summary>
    /// The original aligned rectangle before rotation.
    /// </summary>
    public AlignedRectangle OriginalAlignedRectangle { get; }
    public float RotationRadians { get; }
    public Vector2 Center { get; }

    /// <summary>
    /// The rectangle's corners in counterclockwise order, beginning with the
    /// rotated position of the original bottom-left corner.
    /// </summary>
    public ReadOnlySpan<Vector2> Corners => _corners;

    /// <summary>
    /// The axis-aligned bounding rectangle of the rotated rectangle.
    /// </summary>
    public AlignedRectangle BoundingRectangle => AlignedRectangle.FromPoints(_corners);
    private Matrix3x2 InverseRotationMatrix { get; }

    public readonly bool Contains(Vector2 point)
    {
        // Rotate the point into the rectangle's local space
        var transformedPoint = Vector2.Transform(point, InverseRotationMatrix);

        // Check if the transformed point is inside the aligned rectangle
        return OriginalAlignedRectangle.Contains(transformedPoint);
    }

    public readonly IntersectionRelation GetIntersectionRelation(AlignedRectangle other) =>
        ConvexQueries.GetIntersection(Corners, other.Corners).Relation;

    public readonly bool Overlaps(AlignedRectangle other) =>
        GetIntersectionRelation(other) == IntersectionRelation.Overlapping;

    public readonly bool Touches(AlignedRectangle other) =>
        GetIntersectionRelation(other) == IntersectionRelation.Touching;

    public readonly bool OverlapsOrTouches(AlignedRectangle other) =>
        GetIntersectionRelation(other) != IntersectionRelation.Disjoint;

    public readonly IntersectionRelation GetIntersectionRelation(RotatedRectangle other) =>
        ConvexQueries.GetIntersection(Corners, other.Corners).Relation;

    public readonly bool Overlaps(RotatedRectangle other) =>
        GetIntersectionRelation(other) == IntersectionRelation.Overlapping;

    public readonly bool Touches(RotatedRectangle other) =>
        GetIntersectionRelation(other) == IntersectionRelation.Touching;

    public readonly bool OverlapsOrTouches(RotatedRectangle other) =>
        GetIntersectionRelation(other) != IntersectionRelation.Disjoint;

    /// <summary>
    /// Returns whether two rotated rectangles overlap and, when they do, the
    /// shortest translation that moves this rectangle out of the other.
    /// </summary>
    public readonly bool Overlaps(RotatedRectangle other,
                                  out Vector2 minimumTranslation)
    {
        var intersection = ConvexQueries.GetIntersection(Corners, other.Corners);
        minimumTranslation = intersection.MinimumTranslation;
        return intersection.Relation == IntersectionRelation.Overlapping;
    }

    /// <summary>
    /// Classifies a finite line segment against the rectangle, treating boundary-
    /// only contact as touching and any segment portion inside as overlapping.
    /// </summary>
    public readonly IntersectionRelation GetIntersectionRelation(LineSegment segment)
    {
        var localStart = Vector2.Transform(segment.Start, InverseRotationMatrix);
        var localEnd = Vector2.Transform(segment.End, InverseRotationMatrix);
        var movement = localEnd - localStart;
        Span<Vector2> segmentPoints = stackalloc Vector2[2];
        segmentPoints[0] = segment.Start;
        segmentPoints[1] = segment.End;
        var distanceTolerance = GeometryTolerance.GetDistance(Corners, segmentPoints);
        var contactBounds = OriginalAlignedRectangle.Dilate(distanceTolerance);
        var timeTolerance = GeometryTolerance.GetTime(distanceTolerance, movement);

        if (!SweepMath.TryGetPathBoundsIntersection(localStart,
                                                    movement,
                                                    contactBounds,
                                                    timeTolerance,
                                                    out var intersection))
        {
            return IntersectionRelation.Disjoint;
        }

        var entryTime = Max(0f, Min(intersection.EntryTime, 1f));
        var exitTime = Max(0f, Min(intersection.ExitTime, 1f));
        var middle = localStart + movement * ((entryTime + exitTime) / 2f);
        var overlapsInterior = middle.X > OriginalAlignedRectangle.Left + distanceTolerance &&
                               middle.X < OriginalAlignedRectangle.Right - distanceTolerance &&
                               middle.Y > OriginalAlignedRectangle.Bottom + distanceTolerance &&
                               middle.Y < OriginalAlignedRectangle.Top - distanceTolerance;

        return overlapsInterior
            ? IntersectionRelation.Overlapping
            : IntersectionRelation.Touching;
    }

    public readonly bool Overlaps(LineSegment segment) =>
        GetIntersectionRelation(segment) == IntersectionRelation.Overlapping;

    public readonly bool Touches(LineSegment segment) =>
        GetIntersectionRelation(segment) == IntersectionRelation.Touching;

    public readonly bool OverlapsOrTouches(LineSegment segment) =>
        GetIntersectionRelation(segment) != IntersectionRelation.Disjoint;

    public readonly Vector2 GetClosestPointOnEdge(Vector2 point) =>
        ConvexQueries.GetClosestBoundaryFeature(Corners, point).Point;

    public readonly RotatedRectangle Dilate(float radius)
    {
        var dilatedAlignedRect = OriginalAlignedRectangle.Dilate(radius);
        return new RotatedRectangle(dilatedAlignedRect, RotationRadians);
    }
}
