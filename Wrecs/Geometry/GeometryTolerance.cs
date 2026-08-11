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

    public static float GetDistance(ReadOnlySpan<Vector2> first,
                                    ReadOnlySpan<Vector2> second)
    {
        var coordinateScale = Max(1f,
                                  Max(GetCoordinateScale(first),
                                      GetCoordinateScale(second)));

        var representableStep = BitIncrement(coordinateScale) - coordinateScale;
        return representableStep * RepresentableStepMultiplier;
    }

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

    public static float GetDirection(float distanceTolerance,
                                     Vector2 movement,
                                     float featureScale) =>
        distanceTolerance * movement.Length() /
        Max(Abs(featureScale), 1f);

    public static float GetTime(float distanceTolerance, Vector2 movement) =>
        distanceTolerance / Max(movement.Length(), 1f);
}
