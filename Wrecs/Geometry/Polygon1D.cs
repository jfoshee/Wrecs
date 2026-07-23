namespace Wrecs.Geometry;

static class Polygon1D
{
    /// <summary>
    /// Determines if two polygons are overlapping
    /// when both are projected onto a given axis.
    /// Also calculates the amount of overlap.
    /// </summary>
    /// <param name="poly1">The first polygon represented as an array of Vector2 points.</param>
    /// <param name="poly2">The second polygon represented as an array of Vector2 points.</param>
    /// <param name="axis">The axis to test for overlap, represented as a Vector2.</param>
    /// <param name="overlap">The amount of overlap between the two polygons on the given axis.</param>
    /// <returns>True if the polygons are overlapping on the given axis, otherwise false.</returns>
    public static bool IsOverlappingOnAxis(Vector2[] poly1, Vector2[] poly2, Vector2 axis, out float overlap)
    {
        // Normalize the axis to avoid numerical errors
        axis = Vector2.Normalize(axis);

        // Project both polygons onto the axis
        ProjectPolygonOntoAxis(poly1, axis, out float min1, out float max1);
        ProjectPolygonOntoAxis(poly2, axis, out float min2, out float max2);

        // Check for overlap
        if (max1 < min2 || max2 < min1)
        {
            overlap = 0;
            return false; // Separating axis found
        }
        else
        {
            // Calculate the amount of overlap
            overlap = MathF.Min(max1, max2) - MathF.Max(min1, min2);
            return true;
        }
    }

    private static void ProjectPolygonOntoAxis(Vector2[] poly, Vector2 axis, out float min, out float max)
    {
        float projection = Vector2.Dot(poly[0], axis);
        min = max = projection;

        for (int i = 1; i < poly.Length; i++)
        {
            projection = Vector2.Dot(poly[i], axis);
            if (projection < min)
                min = projection;
            else if (projection > max)
                max = projection;
        }
    }
}
