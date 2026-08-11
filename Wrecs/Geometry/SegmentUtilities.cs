namespace Wrecs.Geometry;

public static class SegmentUtilities
{
    /// <summary>
    /// Determines whether any edges of two polygons (defined by their vertices) intersect.
    /// Each polygon may have a different number of vertices.
    /// </summary>
    /// <param name="vertices1">The vertices of the first polygon.</param>
    /// <param name="vertices2">The vertices of the second polygon.</param>
    /// <returns>True if any edges of the two polygons intersect, otherwise false.</returns>
    public static bool AnyEdgesIntersect(ReadOnlySpan<Vector2> vertices1,
                                         ReadOnlySpan<Vector2> vertices2)
    {
        for (var i = 0; i < vertices1.Length; i++)
        {
            for (var j = 0; j < vertices2.Length; j++)
            {
                if (SegmentsIntersect(vertices1[i],
                                      vertices1[(i + 1) % vertices1.Length],
                                      vertices2[j],
                                      vertices2[(j + 1) % vertices2.Length]))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Determines if the line segment p1q1 intersects with the line segment p2q2.
    /// It handles both the general and special cases of intersection, including collinear points.
    /// <see href="https://www.geeksforgeeks.org/check-if-two-given-line-segments-intersect/"/>
    /// <see href="http://www.dcs.gla.ac.uk/~pat/52233/slides/Geometry1x1.pdf"/>
    /// </summary>
    /// <param name="p1">The starting point of the first line segment.</param>
    /// <param name="q1">The ending point of the first line segment.</param>
    /// <param name="p2">The starting point of the second line segment.</param>
    /// <param name="q2">The ending point of the second line segment.</param>
    /// <returns>True if the two line segments intersect, otherwise false.</returns>
    public static bool SegmentsIntersect(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
    {
        // Find the four orientations needed for general and special cases
        float o1 = Orientation(p1, q1, p2);
        float o2 = Orientation(p1, q1, q2);
        float o3 = Orientation(p2, q2, p1);
        float o4 = Orientation(p2, q2, q1);

        // General case
        if (o1 != o2 && o3 != o4)
        {
            return true;
        }

        // Special cases
        // p1, q1 and p2 are collinear and p2 lies on segment p1q1
        if (o1 == 0 && CollinearOnSegment(p1, p2, q1)) return true;

        // p1, q1 and q2 are collinear and q2 lies on segment p1q1
        if (o2 == 0 && CollinearOnSegment(p1, q2, q1)) return true;

        // p2, q2 and p1 are collinear and p1 lies on segment p2q2
        if (o3 == 0 && CollinearOnSegment(p2, p1, q2)) return true;

        // p2, q2 and q1 are collinear and q1 lies on segment p2q2
        if (o4 == 0 && CollinearOnSegment(p2, q1, q2)) return true;

        return false; // Doesn't fall in any of the above cases
    }

    /// <summary>
    /// Determines the orientation of the ordered triplet (p, q, r).
    /// Returns:
    /// 0 -> p, q, and r are collinear.
    /// 1 -> Clockwise orientation (right turn).
    /// -1 -> Counterclockwise orientation (left turn).
    /// <see href="https://www.geeksforgeeks.org/orientation-3-ordered-points/"/>
    /// </summary>
    /// <param name="p">The first point.</param>
    /// <param name="q">The second point.</param>
    /// <param name="r">The third point.</param>
    /// <returns>
    /// A float representing the orientation:
    /// 0 if collinear, 1 if clockwise, and -1 if counterclockwise.
    /// </returns>
    public static float Orientation(Vector2 p, Vector2 q, Vector2 r)
    {
        float val = (q.Y - p.Y) * (r.X - q.X) - (q.X - p.X) * (r.Y - q.Y);
        return MathF.Sign(val);
    }

    /// <summary>
    /// Determines whether the point q lies on the line segment defined by points p and r.
    /// This assumes that p, q, and r are collinear.
    /// </summary>
    /// <param name="p">The start point of the segment.</param>
    /// <param name="q">The point to check if it lies on the segment.</param>
    /// <param name="r">The end point of the segment.</param>
    /// <returns>True if q lies on the segment defined by p and r, otherwise false.</returns>
    public static bool CollinearOnSegment(Vector2 p, Vector2 q, Vector2 r)
    {
        return q.X <= MathF.Max(p.X, r.X) && q.X >= MathF.Min(p.X, r.X) &&
               q.Y <= MathF.Max(p.Y, r.Y) && q.Y >= MathF.Min(p.Y, r.Y);
    }


    /// <summary>
    /// Determines the closest point on a line segment to a given point.
    /// </summary>
    /// <param name="p1">The start point of the line segment.</param>
    /// <param name="p2">The end point of the line segment.</param>
    /// <param name="point">The point to find the closest point to.</param>
    public static Vector2 GetClosestPointOnLineSegment(Vector2 p1, Vector2 p2, Vector2 point)
    {
        Vector2 edge = p2 - p1;
        float edgeLengthSquared = edge.LengthSquared();

        // Handle degenerate case where the segment length is zero
        if (edgeLengthSquared == 0) return p1;

        // Project p onto the edge, but clamp it within the segment [p1, p2]
        float t = Vector2.Dot(point - p1, edge) / edgeLengthSquared;
        t = Math.Clamp(t, 0, 1);

        // Calculate the closest point based on clamped t
        return p1 + t * edge;
    }
}
