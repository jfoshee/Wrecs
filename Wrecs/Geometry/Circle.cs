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
        var radius = Radius;
        var movement = destination.Center - startCenter;
        var closestAtStart = new LineSegment(segment.Start,
                                             segment.End).GetClosestPoint(startCenter);
        var startOffset = startCenter - closestAtStart;
        var startDistanceSquared = startOffset.LengthSquared();
        var startDistance = MathF.Sqrt(startDistanceSquared);
        var radiusSquared = radius * radius;
        var distanceTolerance = GeometryTolerance.GetDistance(Center,
                                                              segment.Bounds,
                                                              radius);
        var touching = MathF.Abs(startDistance - radius) <= distanceTolerance;
        var startNormal = startOffset == Vector2.Zero
            ? Vector2.Zero
            : Vector2.Normalize(startOffset);
        var directionTolerance = GeometryTolerance.GetDirection(distanceTolerance,
                                                                movement,
                                                                radius);
        var initialContact = SweepMath.ClassifyInitialContact(
            overlapping: startDistanceSquared < radiusSquared && !touching,
            touching,
            movement,
            startNormal,
            directionTolerance);

        if (initialContact.IsResolved)
        {
            hit = initialContact.Hit;
            return initialContact.HasHit;
        }

        var timeTolerance = GeometryTolerance.GetTime(distanceTolerance, movement);
        var hits = new SweepHitAccumulator(timeTolerance);

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
                        hits.Consider(time, -MathF.Sign(movement.Y) * Vector2.UnitY);
                    break;
                }
            case Axis2.Y when movement.X != 0f:
                {
                    var faceX = segment.Anchor.X - MathF.Sign(movement.X) * Radius;
                    var time = (faceX - Center.X) / movement.X;
                    var y = Center.Y + movement.Y * time;
                    if (y >= segment.Interval.Min && y <= segment.Interval.Max)
                        hits.Consider(time, -MathF.Sign(movement.X) * Vector2.UnitX);
                    break;
                }
        }

        ConsiderEndpoint(segment.Start);
        ConsiderEndpoint(segment.End);

        return hits.TryGetHit(out hit);

        void ConsiderEndpoint(Vector2 endpoint)
        {
            if (!SweepMath.TryGetRayCircleEntryTime(startCenter,
                                                    movement,
                                                    endpoint,
                                                    radius,
                                                    out var time) ||
                !hits.IsInRange(time))
            {
                return;
            }

            var contactCenter = startCenter + movement * time;
            var offset = contactCenter - endpoint;
            var normal = offset == Vector2.Zero
                ? Vector2.Zero
                : Vector2.Normalize(offset);
            hits.Consider(time, normal);
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
        var distanceTolerance = GeometryTolerance.GetDistance(Center,
                                                              polygon.Bounds,
                                                              Radius);
        var centerInside = ConvexQueries.ContainsPoint(vertices, normals, Center);
        var closestFeature = ConvexQueries.GetClosestBoundaryFeature(vertices, Center);
        var closestOffset = Center - closestFeature.Point;
        var minimumDistanceSquared = closestOffset.LengthSquared();
        var minimumDistance = MathF.Sqrt(minimumDistanceSquared);
        var touching = !centerInside &&
            MathF.Abs(minimumDistance - Radius) <= distanceTolerance;
        var startNormal = closestOffset == Vector2.Zero
            ? Vector2.Zero
            : Vector2.Normalize(closestOffset);
        var directionTolerance = GeometryTolerance.GetDirection(distanceTolerance,
                                                                movement,
                                                                Radius);
        var initialContact = SweepMath.ClassifyInitialContact(
            overlapping: centerInside ||
                         minimumDistanceSquared < radiusSquared && !touching,
            touching,
            movement,
            startNormal,
            directionTolerance);

        if (initialContact.IsResolved)
        {
            hit = initialContact.Hit;
            return initialContact.HasHit;
        }

        var movementLengthSquared = movement.LengthSquared();
        if (movementLengthSquared == 0f)
        {
            hit = default;
            return false;
        }

        var timeTolerance = GeometryTolerance.GetTime(distanceTolerance, movement);
        var hits = new SweepHitAccumulator(timeTolerance);

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

                if (hits.IsInRange(faceTime))
                {
                    var contact = Center + movement * faceTime - normal * Radius;
                    var edgeProjection = Vector2.Dot(contact - vertex, edge);

                    if (edgeProjection >= 0f && edgeProjection <= edge.LengthSquared())
                        hits.Consider(faceTime, normal);
                }
            }

            if (!SweepMath.TryGetRayCircleEntryTime(Center,
                                                    movement,
                                                    vertex,
                                                    Radius,
                                                    out var vertexTime) ||
                !hits.IsInRange(vertexTime))
            {
                continue;
            }

            var vertexOffset = Center + movement * vertexTime - vertex;
            var vertexNormal = vertexOffset == Vector2.Zero
                ? Vector2.Zero
                : Vector2.Normalize(vertexOffset);
            hits.Consider(vertexTime, vertexNormal);
        }

        return hits.TryGetHit(out hit);
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

}
