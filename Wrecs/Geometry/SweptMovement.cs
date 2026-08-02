namespace Wrecs.Geometry;

internal delegate bool TrySweepIntersection<TCollider>(TCollider source,
                                                       TCollider destination,
                                                       AxisAlignedSegment2 segment,
                                                       out SweepHit hit);

/// <summary>
/// Shared movement resolution for translating colliders that can sweep against
/// axis-aligned segments.
/// </summary>
internal static class SweptMovement
{
    public static Vector2 GetAllowedSlidingMovement<TCollider>(TCollider start,
                                                               Vector2 requestedMovement,
                                                               IEnumerable<AxisAlignedSegment2> segments,
                                                               Func<TCollider, Vector2, TCollider> translate,
                                                               TrySweepIntersection<TCollider> trySweepIntersection,
                                                               float clearance,
                                                               int maxIterations,
                                                               float minimumMovement)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxIterations, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(minimumMovement, 0f);

        var minimumMovementSquared = minimumMovement * minimumMovement;
        var resolvedMovement = Vector2.Zero;
        var remainingMovement = requestedMovement;
        var current = start;

        for (var i = 0; i < maxIterations; i++)
        {
            if (remainingMovement.LengthSquared() <= minimumMovementSquared)
                break;

            var destination = translate(current, remainingMovement);
            if (!TryFindFirstHit(
                    current,
                    destination,
                    segments,
                    trySweepIntersection,
                    out var hit))
            {
                resolvedMovement += remainingMovement;
                break;
            }

            var allowedStep = hit.GetAllowedMovement(remainingMovement, clearance);
            resolvedMovement += allowedStep;
            current = translate(current, allowedStep);

            var blockedStep = remainingMovement - allowedStep;
            if (blockedStep.LengthSquared() <= minimumMovementSquared ||
                hit.Normal == Vector2.Zero)
            {
                break;
            }

            var tangentStep = blockedStep -
                Vector2.Dot(blockedStep, hit.Normal) * hit.Normal;
            if (tangentStep.LengthSquared() <= minimumMovementSquared)
                break;

            remainingMovement = tangentStep;
        }

        return resolvedMovement;
    }

    private static bool TryFindFirstHit<TCollider>(TCollider source,
                                                   TCollider destination,
                                                   IEnumerable<AxisAlignedSegment2> segments,
                                                   TrySweepIntersection<TCollider> trySweepIntersection,
                                                   out SweepHit firstHit)
    {
        firstHit = default;
        var foundHit = false;

        foreach (var segment in segments)
        {
            if (!trySweepIntersection(source, destination, segment, out var hit))
                continue;

            if (!foundHit || hit.Time < firstHit.Time)
            {
                firstHit = hit;
                foundHit = true;
            }
        }

        return foundHit;
    }
}
