namespace Wrecs.Geometry;

public record struct Circle(Vector2 Center, float Radius)
{
    public readonly float Diameter => 2 * Radius;

    public readonly AlignedRectangle Bounds => AlignedRectangle.Centered(Center, Diameter);

    /// <summary>
    /// Finds the first point at which this circle touches an axis-aligned segment
    /// while translating to <paramref name="destination"/>.
    /// </summary>
    /// <remarks>
    /// Expanding the segment by the circle radius produces a capsule. The circle
    /// center is swept against the capsule's rectangular body and two circular
    /// endpoint caps, retaining the earliest contact.
    /// </remarks>
    public readonly bool TrySweepIntersection(
        Circle destination,
        AxisAlignedSegment2 segment,
        out CircleSweepHit hit)
    {
        if (Radius != destination.Radius)
        {
            throw new ArgumentException(
                "Destination must have the same radius as the source circle.",
                nameof(destination));
        }

        var startCenter = Center;
        var movement = destination.Center - startCenter;
        var closestAtStart = ClosestPoint(segment, startCenter);
        var startOffset = startCenter - closestAtStart;
        var startDistanceSquared = startOffset.LengthSquared();
        var radiusSquared = Radius * Radius;

        if (startDistanceSquared < radiusSquared)
        {
            hit = new CircleSweepHit(0f, Center, Vector2.Zero);
            return true;
        }

        if (startDistanceSquared == radiusSquared)
        {
            var startNormal = startOffset == Vector2.Zero
                ? Vector2.Zero
                : Vector2.Normalize(startOffset);

            if (movement == Vector2.Zero)
            {
                hit = new CircleSweepHit(0f, Center, startNormal);
                return true;
            }

            // Existing contact does not block separation or tangent movement.
            if (startNormal == Vector2.Zero || Vector2.Dot(movement, startNormal) >= 0f)
            {
                hit = default;
                return false;
            }

            hit = new CircleSweepHit(0f, Center, startNormal);
            return true;
        }

        var foundHit = false;
        var firstHit = default(CircleSweepHit);

        void Consider(float time, Vector2 normal)
        {
            if (time < 0f || time > 1f)
                return;

            if (!foundHit || time < firstHit.Time)
            {
                foundHit = true;
                firstHit = new CircleSweepHit(
                    time,
                    startCenter + movement * time,
                    normal);
            }
        }

        // Test the two flat sides of the expanded segment. Only the side facing
        // the direction of travel can be the entry face.
        switch (segment.Axis)
        {
            case Axis2.X when movement.Y != 0f:
                {
                    var faceY = segment.Anchor.Y - MathF.Sign(movement.Y) * Radius;
                    var time = (faceY - Center.Y) / movement.Y;
                    var x = Center.X + movement.X * time;
                    if (x >= segment.Interval.Min && x <= segment.Interval.Max)
                        Consider(time, -MathF.Sign(movement.Y) * Vector2.UnitY);
                    break;
                }
            case Axis2.Y when movement.X != 0f:
                {
                    var faceX = segment.Anchor.X - MathF.Sign(movement.X) * Radius;
                    var time = (faceX - Center.X) / movement.X;
                    var y = Center.Y + movement.Y * time;
                    if (y >= segment.Interval.Min && y <= segment.Interval.Max)
                        Consider(time, -MathF.Sign(movement.X) * Vector2.UnitX);
                    break;
                }
        }

        ConsiderEndpoint(segment.Start);
        ConsiderEndpoint(segment.End);

        hit = firstHit;
        return foundHit;

        void ConsiderEndpoint(Vector2 endpoint)
        {
            var a = movement.LengthSquared();
            if (a == 0f)
                return;

            var relativeStart = startCenter - endpoint;
            var b = 2f * Vector2.Dot(relativeStart, movement);
            var c = relativeStart.LengthSquared() - radiusSquared;
            var discriminant = b * b - 4f * a * c;
            if (discriminant < 0f)
                return;

            var time = (-b - MathF.Sqrt(discriminant)) / (2f * a);
            if (time < 0f || time > 1f)
                return;

            var contactCenter = startCenter + movement * time;
            var offset = contactCenter - endpoint;
            var normal = offset == Vector2.Zero
                ? Vector2.Zero
                : Vector2.Normalize(offset);
            Consider(time, normal);
        }
    }

    /// <summary>
    /// Resolves movement against axis-aligned segments by repeatedly sweeping and
    /// preserving the component tangent to the first contacted wall.
    /// </summary>
    public readonly Vector2 GetAllowedSlidingMovement(
        Vector2 requestedMovement,
        IEnumerable<AxisAlignedSegment2> segments,
        float clearance = 0f,
        int maxIterations = 6,
        float minimumMovement = 0.00001f)
    {
        if (maxIterations < 1)
            throw new ArgumentOutOfRangeException(nameof(maxIterations));
        if (minimumMovement < 0f)
            throw new ArgumentOutOfRangeException(nameof(minimumMovement));

        var minimumMovementSquared = minimumMovement * minimumMovement;
        var resolvedMovement = Vector2.Zero;
        var remainingMovement = requestedMovement;
        var current = this;

        for (var i = 0; i < maxIterations; i++)
        {
            if (remainingMovement.LengthSquared() <= minimumMovementSquared)
                break;

            var destination = current with { Center = current.Center + remainingMovement };
            if (!current.TryFindFirstHit(destination, segments, out var hit))
            {
                resolvedMovement += remainingMovement;
                break;
            }

            var allowedStep = hit.GetAllowedMovement(remainingMovement, clearance);
            resolvedMovement += allowedStep;
            current = current with { Center = current.Center + allowedStep };

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

    private readonly bool TryFindFirstHit(
        Circle destination,
        IEnumerable<AxisAlignedSegment2> segments,
        out CircleSweepHit firstHit)
    {
        firstHit = default;
        var foundHit = false;

        foreach (var segment in segments)
        {
            if (!TrySweepIntersection(destination, segment, out var hit))
                continue;

            if (!foundHit || hit.Time < firstHit.Time)
            {
                firstHit = hit;
                foundHit = true;
            }
        }

        return foundHit;
    }

    private static Vector2 ClosestPoint(AxisAlignedSegment2 segment, Vector2 point)
    {
        return segment.Axis switch
        {
            Axis2.X => new Vector2(
                Math.Clamp(point.X, segment.Interval.Min, segment.Interval.Max),
                segment.Anchor.Y),
            Axis2.Y => new Vector2(
                segment.Anchor.X,
                Math.Clamp(point.Y, segment.Interval.Min, segment.Interval.Max)),
            _ => throw new InvalidOperationException($"Unsupported axis: {segment.Axis}.")
        };
    }
}

/// <summary>
/// Describes the first contact found while sweeping a circle.
/// </summary>
public readonly record struct CircleSweepHit(
    float Time,
    Vector2 ContactCenter,
    Vector2 Normal)
{
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
