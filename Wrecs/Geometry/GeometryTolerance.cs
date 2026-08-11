using static System.MathF;

namespace Wrecs.Geometry;

/// <summary>
/// Builds small comparison margins appropriate to a geometry query's coordinate
/// scale. Floating-point numbers become farther apart as their magnitude grows,
/// so calculations far from the origin need a larger margin than calculations
/// near it. Distance, direction, and normalized time have different units and
/// therefore use separate margins.
/// </summary>
internal static class GeometryTolerance
{
    private const float RepresentableStepMultiplier = 8f;

    /// <summary>
    /// Returns a world-distance margin scaled to a point, bounds, and optional
    /// feature size.
    /// </summary>
    public static float GetDistance(Vector2 point,
                                    AlignedRectangle bounds,
                                    float featureScale = 0f)
    {
        var pointScale = Max(Abs(point.X), Abs(point.Y));
        var boundsScale = Max(Max(Abs(bounds.Left), Abs(bounds.Right)),
                              Max(Abs(bounds.Bottom), Abs(bounds.Top)));
        var coordinateScale = Max(1f,
                                  Max(Max(pointScale, boundsScale),
                                      Abs(featureScale)));
        var representableStep = BitIncrement(coordinateScale) - coordinateScale;

        return representableStep * RepresentableStepMultiplier;
    }

    /// <summary>
    /// Returns a world-distance margin scaled to one collection of coordinates.
    /// </summary>
    public static float GetDistance(ReadOnlySpan<Vector2> points)
    {
        var coordinateScale = Max(1f, GetCoordinateScale(points));
        var representableStep = BitIncrement(coordinateScale) - coordinateScale;
        return representableStep * RepresentableStepMultiplier;
    }

    /// <summary>
    /// Returns a world-distance margin scaled to the coordinates of two shapes.
    /// </summary>
    public static float GetDistance(ReadOnlySpan<Vector2> first,
                                    ReadOnlySpan<Vector2> second)
    {
        var coordinateScale = Max(1f,
                                  Max(GetCoordinateScale(first),
                                      GetCoordinateScale(second)));

        var representableStep = BitIncrement(coordinateScale) - coordinateScale;
        return representableStep * RepresentableStepMultiplier;
    }

    /// <summary>
    /// Returns the largest absolute coordinate in a collection of vertices.
    /// </summary>
    private static float GetCoordinateScale(ReadOnlySpan<Vector2> vertices)
    {
        var coordinateScale = 0f;

        foreach (var vertex in vertices)
        {
            coordinateScale = Max(
                coordinateScale,
                Max(Abs(vertex.X), Abs(vertex.Y)));
        }

        return coordinateScale;
    }

    /// <summary>
    /// Converts a world-distance margin into a dot-product margin for comparing
    /// movement with a surface normal.
    /// </summary>
    public static float GetDirection(float distanceTolerance,
                                     Vector2 movement,
                                     float featureScale) =>
        distanceTolerance * movement.Length() /
        Max(Abs(featureScale), 1f);

    /// <summary>
    /// Converts a world-distance margin into normalized movement time, where the
    /// complete movement spans zero through one.
    /// </summary>
    public static float GetTime(float distanceTolerance, Vector2 movement) =>
        distanceTolerance / Max(movement.Length(), 1f);
}
