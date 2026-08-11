using static System.MathF;

namespace Wrecs.Geometry;

internal readonly record struct InitialSweepResult(bool IsResolved,
                                                   bool HasHit,
                                                   SweepHit Hit);

/// <summary>
/// The normalized time interval during which a moving point lies inside an
/// axis-aligned rectangle.
/// </summary>
/// <param name="EntryTime">When the point first enters the rectangle.</param>
/// <param name="ExitTime">When the point leaves the rectangle.</param>
/// <param name="EntryNormal">
/// The outward normal of the crossed entry face. Simultaneous X and Y entry
/// produces a diagonal corner normal.
/// </param>
internal readonly record struct PathBoundsIntersection(float EntryTime,
                                                       float ExitTime,
                                                       Vector2 EntryNormal)
{
    /// <summary>
    /// Converts the interval entry into a collision hit on the finite path.
    /// </summary>
    public SweepHit EntryHit => new(Max(0f, Min(EntryTime, 1f)), EntryNormal);
}

/// <summary>
/// Retains the earliest valid hit from a set of sweep candidates.
/// </summary>
internal struct SweepHitAccumulator(float timeTolerance = 0f)
{
    private SweepHit _hit;

    public bool HasHit { get; private set; }

    public readonly bool IsInRange(float time) =>
        time >= -timeTolerance && time <= 1f + timeTolerance;

    public void Consider(float time, Vector2 normal)
    {
        if (!IsInRange(time))
            return;

        time = Max(0f, Min(time, 1f));
        if (HasHit && time >= _hit.Time)
            return;

        _hit = new SweepHit(time, normal);
        HasHit = true;
    }

    public void Consider(SweepHit hit) => Consider(hit.Time, hit.Normal);

    public readonly bool TryGetHit(out SweepHit hit)
    {
        hit = _hit;
        return HasHit;
    }
}

/// <summary>
/// Shared low-level calculations for finite collider sweeps.
/// </summary>
internal static class SweepMath
{
    public static InitialSweepResult ClassifyInitialContact(bool overlapping,
                                                            bool touching,
                                                            Vector2 movement,
                                                            Vector2 normal,
                                                            float directionTolerance = 0f)
    {
        if (overlapping)
        {
            return new InitialSweepResult(IsResolved: true,
                                          HasHit: true,
                                          Hit: new SweepHit(0f, Vector2.Zero));
        }

        if (!touching)
            return default;

        if (movement == Vector2.Zero)
        {
            return new InitialSweepResult(IsResolved: true,
                                          HasHit: true,
                                          Hit: new SweepHit(0f, normal));
        }

        var separatingOrTangent = normal == Vector2.Zero ||
            Vector2.Dot(movement, normal) >= -directionTolerance;

        return separatingOrTangent
            ? new InitialSweepResult(IsResolved: true,
                                     HasHit: false,
                                     Hit: default)
            : new InitialSweepResult(IsResolved: true,
                                     HasHit: true,
                                     Hit: new SweepHit(0f, normal));
    }

    public static bool TryGetRayCircleEntryTime(Vector2 origin,
                                                Vector2 movement,
                                                Vector2 circleCenter,
                                                float radius,
                                                out float time)
    {
        var movementLengthSquared = movement.LengthSquared();
        if (movementLengthSquared == 0f)
        {
            time = default;
            return false;
        }

        var relativeStart = origin - circleCenter;
        var b = 2f * Vector2.Dot(relativeStart, movement);
        var c = relativeStart.LengthSquared() - radius * radius;
        var discriminant = b * b - 4f * movementLengthSquared * c;
        if (discriminant < 0f)
        {
            time = default;
            return false;
        }

        time = (-b - Sqrt(discriminant)) / (2f * movementLengthSquared);
        return true;
    }

    /// <summary>
    /// Finds the part of a finite point path that lies inside an axis-aligned
    /// rectangle.
    /// </summary>
    /// <remarks>
    /// This is the slab method: first find the times when the path lies within
    /// the rectangle's X range, then do the same for its Y range. The path enters
    /// the rectangle only when those two time intervals overlap.
    /// </remarks>
    /// <param name="start">The point's position at normalized time zero.</param>
    /// <param name="movement">The complete movement from time zero through one.</param>
    /// <param name="bounds">The stationary rectangle to enter.</param>
    /// <param name="timeTolerance">Margin used when comparing calculated times.</param>
    /// <param name="intersection">The shared entry and exit interval when found.</param>
    public static bool TryGetPathBoundsIntersection(Vector2 start,
                                                    Vector2 movement,
                                                    AlignedRectangle bounds,
                                                    float timeTolerance,
                                                    out PathBoundsIntersection intersection)
    {
        var entryTime = 0f;
        var exitTime = 1f;
        var entryNormal = Vector2.Zero;

        if (!RestrictToAxisRange(start.X,
                                 movement.X,
                                 bounds.Left,
                                 bounds.Right,
                                 -Vector2.UnitX,
                                 Vector2.UnitX,
                                 timeTolerance,
                                 ref entryTime,
                                 ref exitTime,
                                 ref entryNormal) ||
            !RestrictToAxisRange(start.Y,
                                 movement.Y,
                                 bounds.Bottom,
                                 bounds.Top,
                                 -Vector2.UnitY,
                                 Vector2.UnitY,
                                 timeTolerance,
                                 ref entryTime,
                                 ref exitTime,
                                 ref entryNormal))
        {
            intersection = default;
            return false;
        }

        if (entryNormal != Vector2.Zero)
            entryNormal = Vector2.Normalize(entryNormal);

        intersection = new PathBoundsIntersection(entryTime,
                                                  exitTime,
                                                  entryNormal);
        return true;
    }

    /// <summary>
    /// Narrows the current path-time interval to the portion inside one
    /// coordinate range.
    /// </summary>
    private static bool RestrictToAxisRange(float startCoordinate,
                                            float coordinateMovement,
                                            float rangeMin,
                                            float rangeMax,
                                            Vector2 negativeFaceNormal,
                                            Vector2 positiveFaceNormal,
                                            float timeTolerance,
                                            ref float entryTime,
                                            ref float exitTime,
                                            ref Vector2 entryNormal)
    {
        if (coordinateMovement == 0f)
            return startCoordinate >= rangeMin && startCoordinate <= rangeMax;

        float axisEntryTime;
        float axisExitTime;
        Vector2 axisEntryNormal;

        if (coordinateMovement > 0f)
        {
            axisEntryTime = (rangeMin - startCoordinate) / coordinateMovement;
            axisExitTime = (rangeMax - startCoordinate) / coordinateMovement;
            axisEntryNormal = negativeFaceNormal;
        }
        else
        {
            axisEntryTime = (rangeMax - startCoordinate) / coordinateMovement;
            axisExitTime = (rangeMin - startCoordinate) / coordinateMovement;
            axisEntryNormal = positiveFaceNormal;
        }

        if (axisEntryTime > entryTime + timeTolerance)
        {
            entryTime = axisEntryTime;
            entryNormal = axisEntryNormal;
        }
        else if (Abs(axisEntryTime - entryTime) <= timeTolerance)
        {
            entryNormal += axisEntryNormal;
        }

        exitTime = Min(exitTime, axisExitTime);
        return entryTime <= exitTime + timeTolerance;
    }
}
