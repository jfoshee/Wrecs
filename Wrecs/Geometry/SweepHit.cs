namespace Wrecs.Geometry;

/// <summary>
/// Describes the first contact found while sweeping a 2D collider against an obstacle.
/// </summary>
/// <param name="Time">
/// The normalized time of contact, where 0 is the starting position and 1 is
/// the requested destination.
/// </param>
/// <param name="Normal">
/// The outward-facing collision normal. This can be <see cref="Vector2.Zero"/>
/// when the sweep starts in an overlap with no unique separating direction.
/// </param>
public readonly record struct SweepHit(float Time,
                                       Vector2 Normal)
{
    /// <summary>
    /// Shortens a requested movement so that it stops at or before contact.
    /// </summary>
    /// <param name="requestedMovement">The complete requested movement.</param>
    /// <param name="clearance">
    /// The distance to leave between the collider and obstacle. A value of zero
    /// stops at exact contact.
    /// </param>
    public Vector2 GetAllowedMovement(Vector2 requestedMovement, float clearance = 0f)
    {
        if (Time <= 0f)
            return Vector2.Zero;

        if (clearance <= 0f || Normal == Vector2.Zero)
            return requestedMovement * Time;

        var approachDistance = -Vector2.Dot(requestedMovement, Normal);
        if (approachDistance <= 0f)
            return requestedMovement * Time;

        var clearanceTime = clearance / approachDistance;
        var allowedTime = MathF.Max(0f, Time - clearanceTime);
        return requestedMovement * allowedTime;
    }
}
