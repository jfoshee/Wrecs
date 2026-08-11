using static System.MathF;

namespace Wrecs.Geometry;

/// <summary>
/// The smallest and largest positions occupied when a shape is projected onto
/// a line. This can be pictured as the shape's one-dimensional shadow on that
/// line.
/// </summary>
/// <param name="Min">The shadow's first occupied position.</param>
/// <param name="Max">The shadow's last occupied position.</param>
internal readonly record struct ProjectionInterval(float Min, float Max);

/// <summary>
/// Identifies the point on a polygon's boundary nearest to a requested point.
/// </summary>
/// <param name="Point">The nearest point on the boundary.</param>
/// <param name="EdgeIndex">The index of the edge containing <paramref name="Point"/>.</param>
/// <param name="EdgeFraction">
/// The position along that edge: zero is its first vertex and one is its second
/// vertex.
/// </param>
internal readonly record struct ClosestBoundaryFeature(Vector2 Point,
                                                       int EdgeIndex,
                                                       float EdgeFraction);

/// <summary>
/// The relationship between two convex shapes and, when they overlap, the
/// shortest translation that moves the first shape out of the second.
/// </summary>
/// <param name="Relation">Whether the shapes are disjoint, touching, or overlapping.</param>
/// <param name="MinimumTranslation">
/// The shortest vector that moves the first shape until it only touches the
/// second. This is zero when the shapes do not overlap.
/// </param>
internal readonly record struct ConvexIntersection(IntersectionRelation Relation,
                                                   Vector2 MinimumTranslation);

/// <summary>
/// Shared queries for convex polygons.
/// </summary>
/// <remarks>
/// <para>
/// A convex polygon has no inward dents: a line drawn between any two points in
/// the polygon stays inside it. The methods here expect at least three vertices,
/// ordered counterclockwise around the boundary, with no zero-length edges.
/// Callers are responsible for satisfying those conditions so these hot query
/// paths do not repeat validation work.
/// </para>
/// <para>
/// Polygon intersection uses the Separating Axis Theorem (SAT). Picture casting
/// each polygon's shadow onto a line. If the two shadows have a gap on any tested
/// line, that line separates the polygons and they are disjoint. For convex
/// polygons, it is sufficient to test the directions perpendicular to every edge
/// of both polygons.
/// </para>
/// </remarks>
internal static class ConvexQueries
{
    /// <summary>
    /// Projects every vertex onto <paramref name="unitAxis"/> and returns the
    /// interval covered by the resulting one-dimensional shadow.
    /// </summary>
    /// <param name="vertices">The polygon vertices to project.</param>
    /// <param name="unitAxis">
    /// The direction of the projection line. It must have length one so interval
    /// distances remain in world-coordinate units.
    /// </param>
    public static ProjectionInterval Project(ReadOnlySpan<Vector2> vertices,
                                             Vector2 unitAxis)
    {
        var projection = Vector2.Dot(vertices[0], unitAxis);
        var min = projection;
        var max = projection;

        for (var i = 1; i < vertices.Length; i++)
        {
            projection = Vector2.Dot(vertices[i], unitAxis);
            min = Min(min, projection);
            max = Max(max, projection);
        }

        return new ProjectionInterval(min, max);
    }

    /// <summary>
    /// Determines whether a point is inside the polygon or on its boundary.
    /// </summary>
    /// <param name="vertices">Counterclockwise polygon vertices.</param>
    /// <param name="outwardNormals">
    /// One unit-length outward normal per edge, in the same order as
    /// <paramref name="vertices"/>.
    /// </param>
    /// <param name="point">The point to classify.</param>
    /// <param name="distanceTolerance">
    /// How far outside an edge the point may be while still being treated as on
    /// the boundary. This is expressed in world-coordinate units.
    /// </param>
    /// <remarks>
    /// Each edge and its outward normal define an outside half-plane. The point
    /// is inside when it is not in the outside half-plane of any edge.
    /// </remarks>
    public static bool ContainsPoint(ReadOnlySpan<Vector2> vertices,
                                     ReadOnlySpan<Vector2> outwardNormals,
                                     Vector2 point,
                                     float distanceTolerance = 0f)
    {
        for (var i = 0; i < vertices.Length; i++)
        {
            if (Vector2.Dot(point - vertices[i], outwardNormals[i]) > distanceTolerance)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Finds the point on the polygon boundary nearest to
    /// <paramref name="point"/>.
    /// </summary>
    /// <remarks>
    /// Every finite edge is checked rather than treating an edge as an infinite
    /// line. Consequently the returned feature may lie in the middle of an edge
    /// or exactly on a vertex.
    /// </remarks>
    public static ClosestBoundaryFeature GetClosestBoundaryFeature(ReadOnlySpan<Vector2> vertices,
                                                                   Vector2 point)
    {
        var closestPoint = default(Vector2);
        var closestEdgeIndex = -1;
        var closestEdgeFraction = 0f;
        var minimumDistanceSquared = float.PositiveInfinity;

        for (var i = 0; i < vertices.Length; i++)
        {
            var vertex = vertices[i];
            var edge = new LineSegment(vertex,
                                       vertices[(i + 1) % vertices.Length]);
            var candidate = edge.GetClosestPoint(point, out var edgeFraction);
            var distanceSquared = Vector2.DistanceSquared(point, candidate);

            if (distanceSquared >= minimumDistanceSquared)
                continue;

            closestPoint = candidate;
            closestEdgeIndex = i;
            closestEdgeFraction = edgeFraction;
            minimumDistanceSquared = distanceSquared;
        }

        return new ClosestBoundaryFeature(closestPoint,
                                          closestEdgeIndex,
                                          closestEdgeFraction);
    }

    /// <summary>
    /// Classifies two convex polygons using the Separating Axis Theorem and
    /// computes the minimum translation needed to separate an overlap.
    /// </summary>
    /// <param name="first">The polygon that the returned translation moves.</param>
    /// <param name="second">The stationary polygon.</param>
    /// <returns>
    /// <see cref="IntersectionRelation.Disjoint"/> when a separating gap exists;
    /// <see cref="IntersectionRelation.Touching"/> when the polygons meet without
    /// interior overlap; otherwise <see cref="IntersectionRelation.Overlapping"/>
    /// with the shortest separating translation.
    /// </returns>
    /// <remarks>
    /// Small projection differences within the coordinate-aware distance
    /// tolerance are treated as touching. This prevents floating-point rounding
    /// from changing an intended shared edge into a tiny gap or overlap.
    /// </remarks>
    public static ConvexIntersection GetIntersection(ReadOnlySpan<Vector2> first,
                                                     ReadOnlySpan<Vector2> second)
    {
        var distanceTolerance = GeometryTolerance.GetDistance(first, second);
        var relation = IntersectionRelation.Overlapping;
        var minimumDepth = float.PositiveInfinity;
        var minimumTranslationDirection = Vector2.Zero;

        if (!TestSeparatingAxes(first,
                                first,
                                second,
                                distanceTolerance,
                                ref relation,
                                ref minimumDepth,
                                ref minimumTranslationDirection) ||
            !TestSeparatingAxes(second,
                                first,
                                second,
                                distanceTolerance,
                                ref relation,
                                ref minimumDepth,
                                ref minimumTranslationDirection))
        {
            return new ConvexIntersection(IntersectionRelation.Disjoint,
                                          Vector2.Zero);
        }

        var minimumTranslation = relation == IntersectionRelation.Overlapping
            ? minimumTranslationDirection * minimumDepth
            : Vector2.Zero;
        return new ConvexIntersection(relation, minimumTranslation);
    }

    /// <summary>
    /// Tests the edge-perpendicular directions supplied by one polygon. SAT must
    /// call this once for each polygon because either shape may contribute the
    /// direction that separates them or produces the shortest translation.
    /// </summary>
    private static bool TestSeparatingAxes(ReadOnlySpan<Vector2> verticesProvidingAxes,
                                           ReadOnlySpan<Vector2> first,
                                           ReadOnlySpan<Vector2> second,
                                           float distanceTolerance,
                                           ref IntersectionRelation relation,
                                           ref float minimumDepth,
                                           ref Vector2 minimumTranslationDirection)
    {
        for (var i = 0; i < verticesProvidingAxes.Length; i++)
        {
            var edge = verticesProvidingAxes[(i + 1) % verticesProvidingAxes.Length] -
                       verticesProvidingAxes[i];
            if (edge == Vector2.Zero)
                continue;

            // Either perpendicular direction describes the same projection
            // line, so the normal does not need to point outside the polygon.
            var axis = Vector2.Normalize(new Vector2(-edge.Y, edge.X));
            var firstProjection = Project(first, axis);
            var secondProjection = Project(second, axis);

            // These expressions measure a possible gap for both left-to-right
            // orderings. A positive value means the shadows are separated.
            var gap = Max(secondProjection.Min - firstProjection.Max,
                          firstProjection.Min - secondProjection.Max);

            if (gap > distanceTolerance)
                return false;

            // Measure how far the first shadow must move in either direction
            // before its nearest end meets an end of the second shadow.
            var depthTowardNegativeAxis = firstProjection.Max - secondProjection.Min;
            var depthTowardPositiveAxis = secondProjection.Max - firstProjection.Min;
            var axisDepth = Min(depthTowardNegativeAxis, depthTowardPositiveAxis);

            if (axisDepth <= distanceTolerance)
            {
                relation = IntersectionRelation.Touching;
                continue;
            }

            if (axisDepth >= minimumDepth)
                continue;

            minimumDepth = axisDepth;
            minimumTranslationDirection = depthTowardNegativeAxis <= depthTowardPositiveAxis
                ? -axis
                : axis;
        }

        return true;
    }
}
