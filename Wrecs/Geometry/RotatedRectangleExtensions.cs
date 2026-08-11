namespace Wrecs.Geometry;

public static class RotatedRectangleExtensions
{
    public static IntersectionRelation GetIntersectionRelation(this RotatedRectangle rectangle,
                                                               RotatedRectangle other) =>
        ConvexQueries.GetIntersection(rectangle.Corners, other.Corners).Relation;

    public static bool Overlaps(this RotatedRectangle rectangle,
                                RotatedRectangle other) =>
        rectangle.GetIntersectionRelation(other) == IntersectionRelation.Overlapping;

    public static bool Touches(this RotatedRectangle rectangle,
                               RotatedRectangle other) =>
        rectangle.GetIntersectionRelation(other) == IntersectionRelation.Touching;

    public static bool OverlapsOrTouches(this RotatedRectangle rectangle,
                                         RotatedRectangle other) =>
        rectangle.GetIntersectionRelation(other) != IntersectionRelation.Disjoint;

    public static bool Overlaps(this RotatedRectangle rectangle,
                                RotatedRectangle other,
                                out Vector2 minimumTranslation)
    {
        var intersection = ConvexQueries.GetIntersection(rectangle.Corners,
                                                         other.Corners);
        minimumTranslation = intersection.MinimumTranslation;
        return intersection.Relation == IntersectionRelation.Overlapping;
    }

    public static Vector2 GetClosestPointOnEdge(this RotatedRectangle rectangle,
                                                Vector2 point) =>
        ConvexQueries.GetClosestBoundaryFeature(rectangle.Corners, point).Point;

    public static bool Intersects(this RotatedRectangle rectangle, LineSegment lineSegment)
    {
        var corners = rectangle.Corners;

        // Check if any of the rectangle's edges intersect with the line segment
        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 p1 = corners[i];
            Vector2 p2 = corners[(i + 1) % corners.Length];

            if (SegmentUtilities.SegmentsIntersect(p1, p2, lineSegment.Start, lineSegment.End))
            {
                return true;
            }
        }

        // Check if the line segment is completely inside the rectangle
        if (rectangle.Contains(lineSegment.Start) || rectangle.Contains(lineSegment.End))
        {
            return true;
        }

        return false;
    }
}
