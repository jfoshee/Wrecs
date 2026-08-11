namespace Wrecs.Geometry;

internal readonly record struct InitialSweepResult(bool IsResolved,
                                                   bool HasHit,
                                                   SweepHit Hit);

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

        time = Math.Clamp(time, 0f, 1f);
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

        time = (-b - MathF.Sqrt(discriminant)) / (2f * movementLengthSquared);
        return true;
    }

    public static bool TryGetRayBoundsHit(Vector2 origin,
                                          Vector2 movement,
                                          AlignedRectangle bounds,
                                          float timeTolerance,
                                          out SweepHit hit,
                                          out float exitTime)
    {
        var entryTime = 0f;
        exitTime = 1f;
        var entryNormal = Vector2.Zero;

        if (!RestrictTimeRangeToAxis(origin.X,
                                     movement.X,
                                     bounds.Left,
                                     bounds.Right,
                                     -Vector2.UnitX,
                                     Vector2.UnitX,
                                     timeTolerance,
                                     ref entryTime,
                                     ref exitTime,
                                     ref entryNormal) ||
            !RestrictTimeRangeToAxis(origin.Y,
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
            hit = default;
            return false;
        }

        if (entryNormal != Vector2.Zero)
            entryNormal = Vector2.Normalize(entryNormal);

        hit = new SweepHit(Math.Clamp(entryTime, 0f, 1f), entryNormal);
        return true;
    }

    private static bool RestrictTimeRangeToAxis(float origin,
                                                float movement,
                                                float rangeMin,
                                                float rangeMax,
                                                Vector2 negativeFaceNormal,
                                                Vector2 positiveFaceNormal,
                                                float timeTolerance,
                                                ref float entryTime,
                                                ref float exitTime,
                                                ref Vector2 entryNormal)
    {
        if (movement == 0f)
            return origin >= rangeMin && origin <= rangeMax;

        float axisEntryTime;
        float axisExitTime;
        Vector2 axisEntryNormal;

        if (movement > 0f)
        {
            axisEntryTime = (rangeMin - origin) / movement;
            axisExitTime = (rangeMax - origin) / movement;
            axisEntryNormal = negativeFaceNormal;
        }
        else
        {
            axisEntryTime = (rangeMax - origin) / movement;
            axisExitTime = (rangeMin - origin) / movement;
            axisEntryNormal = positiveFaceNormal;
        }

        if (axisEntryTime > entryTime + timeTolerance)
        {
            entryTime = axisEntryTime;
            entryNormal = axisEntryNormal;
        }
        else if (MathF.Abs(axisEntryTime - entryTime) <= timeTolerance)
        {
            entryNormal += axisEntryNormal;
        }

        exitTime = MathF.Min(exitTime, axisExitTime);
        return entryTime <= exitTime + timeTolerance;
    }
}
