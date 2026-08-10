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
    public readonly bool TrySweepIntersection(Circle destination,
                                              AxisAlignedSegment2 segment,
                                              out SweepHit hit)
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
            hit = new SweepHit(0f, Vector2.Zero);
            return true;
        }

        if (startDistanceSquared == radiusSquared)
        {
            var startNormal = startOffset == Vector2.Zero
                ? Vector2.Zero
                : Vector2.Normalize(startOffset);

            if (movement == Vector2.Zero)
            {
                hit = new SweepHit(0f, startNormal);
                return true;
            }

            // Existing contact does not block separation or tangent movement.
            if (startNormal == Vector2.Zero || Vector2.Dot(movement, startNormal) >= 0f)
            {
                hit = default;
                return false;
            }

            hit = new SweepHit(0f, startNormal);
            return true;
        }

        var foundHit = false;
        var firstHit = default(SweepHit);

        void Consider(float time, Vector2 normal)
        {
            if (time < 0f || time > 1f)
                return;

            if (!foundHit || time < firstHit.Time)
            {
                foundHit = true;
                firstHit = new SweepHit(time,
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
    /// Finds the first point at which this circle touches a convex polygon while
    /// translating to <paramref name="destination"/>.
    /// </summary>
    /// <remarks>
    /// The polygon is expanded by the circle radius. Its boundary consists of
    /// offset edge faces and circular vertex caps, which are tested directly
    /// without constructing the expanded polygon.
    /// </remarks>
    public readonly bool TrySweepIntersection(Circle destination,
                                              ConvexPolygon polygon,
                                              out SweepHit hit)
    {
        if (Radius != destination.Radius)
        {
            throw new ArgumentException(
                "Destination must have the same radius as the source circle.",
                nameof(destination));
        }

        var vertices = polygon.Vertices;
        var normals = polygon.EdgeNormals;
        var movement = destination.Center - Center;
        var radiusSquared = Radius * Radius;
        var minimumDistanceSquared = float.PositiveInfinity;
        var closestOffset = Vector2.Zero;
        var centerInside = true;

        // Classify the center against the polygon and find its closest boundary
        // point. Together these identify overlap with the radius-expanded polygon.
        for (var i = 0; i < vertices.Length; i++)
        {
            var vertex = vertices[i];
            var edge = vertices[(i + 1) % vertices.Length] - vertex;
            var centerOffset = Center - vertex;

            if (Vector2.Dot(centerOffset, normals[i]) > 0f)
                centerInside = false;

            var edgeFraction = Math.Clamp(Vector2.Dot(centerOffset, edge) /
                                          edge.LengthSquared(),
                                          0f,
                                          1f);
            var offset = centerOffset - edge * edgeFraction;
            var distanceSquared = offset.LengthSquared();

            if (distanceSquared < minimumDistanceSquared)
            {
                minimumDistanceSquared = distanceSquared;
                closestOffset = offset;
            }
        }

        // An overlap has no unique separating normal. Exact contact only blocks
        // movement directed into the polygon; tangent or separating motion is free.
        if (centerInside || minimumDistanceSquared < radiusSquared)
        {
            hit = new SweepHit(0f, Vector2.Zero);
            return true;
        }

        if (minimumDistanceSquared == radiusSquared)
        {
            var startNormal = closestOffset == Vector2.Zero
                ? Vector2.Zero
                : Vector2.Normalize(closestOffset);

            if (movement == Vector2.Zero)
            {
                hit = new SweepHit(0f, startNormal);
                return true;
            }

            if (startNormal == Vector2.Zero || Vector2.Dot(movement, startNormal) >= 0f)
            {
                hit = default;
                return false;
            }

            hit = new SweepHit(0f, startNormal);
            return true;
        }

        var movementLengthSquared = movement.LengthSquared();
        if (movementLengthSquared == 0f)
        {
            hit = default;
            return false;
        }

        var foundHit = false;
        var firstTime = float.PositiveInfinity;
        var firstNormal = Vector2.Zero;

        // Sweep the center against every offset edge face and circular vertex cap,
        // retaining the earliest contact on the radius-expanded boundary.
        for (var i = 0; i < vertices.Length; i++)
        {
            var vertex = vertices[i];
            var nextVertex = vertices[(i + 1) % vertices.Length];
            var edge = nextVertex - vertex;
            var normal = normals[i];
            var normalMovement = Vector2.Dot(movement, normal);

            if (normalMovement < 0f)
            {
                // Intersect the offset edge line, then reject contacts beyond the
                // finite edge; those endpoint regions belong to the vertex caps.
                var faceTime = (Radius - Vector2.Dot(Center - vertex, normal)) /
                               normalMovement;

                if (faceTime >= 0f && faceTime <= 1f && faceTime < firstTime)
                {
                    var contact = Center + movement * faceTime - normal * Radius;
                    var edgeProjection = Vector2.Dot(contact - vertex, edge);

                    if (edgeProjection >= 0f && edgeProjection <= edge.LengthSquared())
                    {
                        foundHit = true;
                        firstTime = faceTime;
                        firstNormal = normal;
                    }
                }
            }

            // Solve the ray-circle quadratic and use its entry root for this cap.
            var relativeStart = Center - vertex;
            var b = 2f * Vector2.Dot(relativeStart, movement);
            var c = relativeStart.LengthSquared() - radiusSquared;
            var discriminant = b * b - 4f * movementLengthSquared * c;

            if (discriminant < 0f)
                continue;

            var vertexTime = (-b - MathF.Sqrt(discriminant)) /
                             (2f * movementLengthSquared);

            if (vertexTime < 0f || vertexTime > 1f || vertexTime >= firstTime)
                continue;

            var vertexOffset = Center + movement * vertexTime - vertex;
            foundHit = true;
            firstTime = vertexTime;
            firstNormal = vertexOffset == Vector2.Zero
                ? Vector2.Zero
                : Vector2.Normalize(vertexOffset);
        }

        hit = foundHit ? new SweepHit(firstTime, firstNormal) : default;
        return foundHit;
    }

    /// <summary>
    /// Resolves movement against axis-aligned segments by repeatedly sweeping and
    /// preserving the component tangent to the first contacted wall.
    /// </summary>
    public readonly Vector2 GetAllowedSlidingMovement(Vector2 requestedMovement,
                                                      IEnumerable<AxisAlignedSegment2> segments,
                                                      float clearance = 0f,
                                                      int maxIterations = 6,
                                                      float minimumMovement = 0.00001f)
    {
        return SweptMovement.GetAllowedSlidingMovement(
            this,
            requestedMovement,
            segments,
            static (circle, movement) => circle with
            {
                Center = circle.Center + movement
            },
            static (Circle source,
                    Circle destination,
                    AxisAlignedSegment2 segment,
                    out SweepHit hit) =>
                source.TrySweepIntersection(destination, segment, out hit),
            clearance,
            maxIterations,
            minimumMovement);
    }

    /// <summary>
    /// Resolves movement against convex polygons by repeatedly sweeping and
    /// preserving the component tangent to the first contacted boundary.
    /// </summary>
    public readonly Vector2 GetAllowedSlidingMovement(Vector2 requestedMovement,
                                                      IEnumerable<ConvexPolygon> polygons,
                                                      float clearance = 0f,
                                                      int maxIterations = 6,
                                                      float minimumMovement = 0.00001f)
    {
        return SweptMovement.GetAllowedSlidingMovement(
            this,
            requestedMovement,
            polygons,
            static (circle, movement) => circle with
            {
                Center = circle.Center + movement
            },
            static (Circle source,
                    Circle destination,
                    ConvexPolygon polygon,
                    out SweepHit hit) =>
                source.TrySweepIntersection(destination, polygon, out hit),
            clearance,
            maxIterations,
            minimumMovement);
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
