namespace Wrecs.Geometry;

public static class RotatedRectangleExtensions
{
    public static bool Intersects(this RotatedRectangle sprite1, RotatedRectangle sprite2, out Vector2 mtv)
    {
        // Initialize the Minimum Translation Vector (MTV)
        float minOverlap = float.MaxValue;
        var mtvAxis = new Vector2();

        // Get the corners of both sprites in world space
        Vector2[] corners1 = sprite1.Corners;
        Vector2[] corners2 = sprite2.Corners;

        // Get the axes (normals to the edges) for both sprites
        Vector2[] axes =
        [
            GetEdgeNormal(corners1[0], corners1[1]),
            GetEdgeNormal(corners1[1], corners1[2]),
            GetEdgeNormal(corners2[0], corners2[1]),
            GetEdgeNormal(corners2[1], corners2[2]),
        ];

        // For each axis, project both sprites and check for overlap
        foreach (var axis in axes)
        {
            if (!Polygon1D.IsOverlappingOnAxis(corners1, corners2, axis, out var overlap))
            {
                // Separating axis found, no collision
                mtv = Vector2.Zero;
                return false;
            }
            else if (overlap < minOverlap)
            {
                // Keep track of the smallest overlap and corresponding axis
                minOverlap = overlap;
                mtvAxis = axis;
            }
        }

        // Collision detected; compute the MTV
        mtvAxis = Vector2.Normalize(mtvAxis);
        mtv = mtvAxis * minOverlap;

        // Ensure MTV pushes sprite1 out of sprite2
        Vector2 direction = sprite1.Center - sprite2.Center;
        if (Vector2.Dot(direction, mtv) < 0)
        {
            mtv = -mtv;
        }

        return true;
    }

    public static Vector2 GetClosestPointOnEdge(this RotatedRectangle rectangle, Vector2 p)
    {
        var corners = rectangle.Corners;
        var closestPoint = Vector2.Zero;
        float minDistanceSquared = float.MaxValue;

        // Loop through each edge of the rectangle
        for (int i = 0; i < corners.Length; i++)
        {
            // Get the current edge as points (p1, p2)
            Vector2 p1 = corners[i];
            Vector2 p2 = corners[(i + 1) % corners.Length];

            // Calculate the closest point on this edge to p
            Vector2 closestPointOnEdge = SegmentUtilities.GetClosestPointOnLineSegment(p1, p2, p);

            // Calculate squared distance to avoid unnecessary sqrt operations
            float distanceSquared = Vector2.DistanceSquared(p, closestPointOnEdge);

            // Update if this is the closest point found so far
            if (distanceSquared < minDistanceSquared)
            {
                minDistanceSquared = distanceSquared;
                closestPoint = closestPointOnEdge;
            }
        }

        return closestPoint;
    }

    private static Vector2 GetEdgeNormal(Vector2 p1, Vector2 p2)
    {
        Vector2 edge = p2 - p1;
        // Return the normal (perpendicular) vector
        return new Vector2(-edge.Y, edge.X);
    }

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
