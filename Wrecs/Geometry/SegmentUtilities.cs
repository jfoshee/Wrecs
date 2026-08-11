namespace Wrecs.Geometry;

/// <summary>
/// Internal helpers for collections of segment edges. Public segment
/// relationships belong on <see cref="LineSegment"/>.
/// </summary>
internal static class SegmentUtilities
{
    /// <summary>
    /// Returns whether any finite boundary edge from one polygon overlaps or
    /// touches a boundary edge from the other polygon.
    /// </summary>
    /// <remarks>
    /// This checks boundary edges only. It returns false when one polygon is
    /// wholly contained in the other without any boundary contact.
    /// </remarks>
    internal static bool AnyEdgesIntersect(ReadOnlySpan<Vector2> firstVertices,
                                           ReadOnlySpan<Vector2> secondVertices)
    {
        for (var i = 0; i < firstVertices.Length; i++)
        {
            var firstEdge = new LineSegment(firstVertices[i],
                                            firstVertices[(i + 1) % firstVertices.Length]);

            for (var j = 0; j < secondVertices.Length; j++)
            {
                var secondEdge = new LineSegment(secondVertices[j],
                                                 secondVertices[(j + 1) % secondVertices.Length]);

                if (firstEdge.OverlapsOrTouches(secondEdge))
                    return true;
            }
        }

        return false;
    }
}
