namespace Wrecs.Geometry;

/// <summary>
/// Builds comparison tolerances from the scale of the coordinates involved in
/// a geometry query. Distance, direction, and normalized time have different
/// units and therefore use separate tolerances.
/// </summary>
internal static class GeometryTolerance
{
    private const float CoordinateUlpMultiplier = 8f;

    public static float GetDistance(Vector2 point,
                                    AlignedRectangle bounds,
                                    float featureScale = 0f)
    {
        var coordinateScale = MathF.Max(
            1f,
            MathF.Max(
                MathF.Max(
                    MathF.Max(MathF.Abs(point.X), MathF.Abs(point.Y)),
                    MathF.Abs(featureScale)),
                MathF.Max(
                    MathF.Max(MathF.Abs(bounds.Left), MathF.Abs(bounds.Right)),
                    MathF.Max(MathF.Abs(bounds.Bottom), MathF.Abs(bounds.Top)))));
        var coordinateUlp = MathF.BitIncrement(coordinateScale) - coordinateScale;

        return coordinateUlp * CoordinateUlpMultiplier;
    }

    public static float GetDirection(float distanceTolerance,
                                     Vector2 movement,
                                     float featureScale) =>
        distanceTolerance * movement.Length() /
        MathF.Max(MathF.Abs(featureScale), 1f);

    public static float GetTime(float distanceTolerance, Vector2 movement) =>
        distanceTolerance / MathF.Max(movement.Length(), 1f);
}
