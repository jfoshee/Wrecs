using static System.MathF;

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
        // 1. Validate that the destination describes translation only.
        var movement = ValidateTranslation(destination);

        // 2. Classify the circle's initial relationship with the segment.
        var closestAtStart = new LineSegment(segment.Start,
                                             segment.End).GetClosestPoint(Center);
        var distanceTolerance = GeometryTolerance.GetDistance(Center,
                                                              segment.Bounds,
                                                              Radius);
        var initialContact = ClassifyInitialRelationship(closestAtStart,
                                                         centerInside: false,
                                                         movement,
                                                         distanceTolerance);

        if (initialContact.IsResolved)
        {
            hit = initialContact.Hit;
            return initialContact.HasHit;
        }

        var hits = CreateHitAccumulator(movement, distanceTolerance);

        // 3. Test the flat faces of the radius-expanded segment.
        TestExpandedFaces(segment, movement, ref hits);

        // 4. Test the circular caps around both endpoints.
        TestCircularCap(segment.Start, movement, ref hits);
        TestCircularCap(segment.End, movement, ref hits);

        // 5. Select the earliest valid face or cap contact.
        return hits.TryGetHit(out hit);
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
        // 1. Validate that the destination describes translation only.
        var movement = ValidateTranslation(destination);

        // 2. Classify the circle's initial relationship with the polygon.
        var vertices = polygon.Vertices;
        var normals = polygon.EdgeNormals;
        var distanceTolerance = GeometryTolerance.GetDistance(Center,
                                                              polygon.Bounds,
                                                              Radius);
        var centerInside = ConvexQueries.ContainsPoint(vertices, normals, Center);
        var closestFeature = ConvexQueries.GetClosestBoundaryFeature(vertices, Center);
        var initialContact = ClassifyInitialRelationship(closestFeature.Point,
                                                         centerInside,
                                                         movement,
                                                         distanceTolerance);

        if (initialContact.IsResolved)
        {
            hit = initialContact.Hit;
            return initialContact.HasHit;
        }

        var hits = CreateHitAccumulator(movement, distanceTolerance);

        // 3. Test the flat faces of the radius-expanded polygon.
        TestExpandedFaces(vertices, normals, movement, ref hits);

        // 4. Test the circular cap around every polygon vertex.
        foreach (var vertex in vertices)
            TestCircularCap(vertex, movement, ref hits);

        // 5. Select the earliest valid face or cap contact.
        return hits.TryGetHit(out hit);
    }

    /// <summary>
    /// Validates that a destination changes only the circle's position and
    /// returns that translation.
    /// </summary>
    private readonly Vector2 ValidateTranslation(Circle destination)
    {
        if (Radius != destination.Radius)
        {
            throw new ArgumentException(
                "Destination must have the same radius as the source circle.",
                nameof(destination));
        }

        return destination.Center - Center;
    }

    /// <summary>
    /// Applies the common initial overlap, touch, and separating-movement policy
    /// to a circle and the closest point on an obstacle's boundary.
    /// </summary>
    private readonly InitialSweepResult ClassifyInitialRelationship(Vector2 closestBoundaryPoint,
                                                                    bool centerInside,
                                                                    Vector2 movement,
                                                                    float distanceTolerance)
    {
        var boundaryOffset = Center - closestBoundaryPoint;
        var boundaryDistanceSquared = boundaryOffset.LengthSquared();
        var boundaryDistance = Sqrt(boundaryDistanceSquared);
        var touching = !centerInside &&
                       Abs(boundaryDistance - Radius) <= distanceTolerance;
        var normal = boundaryOffset == Vector2.Zero
            ? Vector2.Zero
            : Vector2.Normalize(boundaryOffset);
        var directionTolerance = GeometryTolerance.GetDirection(distanceTolerance,
                                                                movement,
                                                                Radius);

        return SweepMath.ClassifyInitialContact(overlapping: centerInside ||
                                                (boundaryDistanceSquared < Radius * Radius &&
                                                 !touching),
                                                touching,
                                                movement,
                                                normal,
                                                directionTolerance);
    }

    /// <summary>
    /// Creates the common earliest-hit collector using a coordinate-aware time
    /// margin.
    /// </summary>
    private static SweepHitAccumulator CreateHitAccumulator(Vector2 movement,
                                                            float distanceTolerance) =>
        new(GeometryTolerance.GetTime(distanceTolerance, movement));

    /// <summary>
    /// Tests the two flat sides of the capsule formed by expanding a segment by
    /// the circle radius.
    /// </summary>
    private readonly void TestExpandedFaces(AxisAlignedSegment2 segment,
                                            Vector2 movement,
                                            ref SweepHitAccumulator hits)
    {
        // Only the face pointing against the direction of travel can be the
        // entry face.
        switch (segment.Axis)
        {
            case Axis2.X when movement.Y != 0f:
                {
                    var normal = -Sign(movement.Y) * Vector2.UnitY;
                    var faceY = segment.Anchor.Y + normal.Y * Radius;
                    var time = (faceY - Center.Y) / movement.Y;
                    var contactX = Center.X + movement.X * time;

                    if (contactX >= segment.Interval.Min &&
                        contactX <= segment.Interval.Max)
                    {
                        hits.Consider(time, normal);
                    }

                    break;
                }
            case Axis2.Y when movement.X != 0f:
                {
                    var normal = -Sign(movement.X) * Vector2.UnitX;
                    var faceX = segment.Anchor.X + normal.X * Radius;
                    var time = (faceX - Center.X) / movement.X;
                    var contactY = Center.Y + movement.Y * time;

                    if (contactY >= segment.Interval.Min &&
                        contactY <= segment.Interval.Max)
                    {
                        hits.Consider(time, normal);
                    }

                    break;
                }
        }
    }

    /// <summary>
    /// Tests every flat face formed by offsetting a polygon edge outward by the
    /// circle radius.
    /// </summary>
    private readonly void TestExpandedFaces(ReadOnlySpan<Vector2> vertices,
                                            ReadOnlySpan<Vector2> normals,
                                            Vector2 movement,
                                            ref SweepHitAccumulator hits)
    {
        for (var i = 0; i < vertices.Length; i++)
        {
            var vertex = vertices[i];
            var edge = vertices[(i + 1) % vertices.Length] - vertex;
            var normal = normals[i];
            var normalMovement = Vector2.Dot(movement, normal);

            if (normalMovement >= 0f)
                continue;

            // Intersect the offset edge line, then reject contacts beyond the
            // finite edge; those endpoint regions belong to the circular caps.
            var faceTime = (Radius - Vector2.Dot(Center - vertex, normal)) /
                           normalMovement;
            if (!hits.IsInRange(faceTime))
                continue;

            var contact = Center + movement * faceTime - normal * Radius;
            var edgeProjection = Vector2.Dot(contact - vertex, edge);

            if (edgeProjection >= 0f && edgeProjection <= edge.LengthSquared())
                hits.Consider(faceTime, normal);
        }
    }

    /// <summary>
    /// Tests one circular endpoint or vertex cap and adds its entry contact to
    /// the earliest-hit collector.
    /// </summary>
    private readonly void TestCircularCap(Vector2 capCenter,
                                          Vector2 movement,
                                          ref SweepHitAccumulator hits)
    {
        if (!SweepMath.TryGetRayCircleEntryTime(Center,
                                                movement,
                                                capCenter,
                                                Radius,
                                                out var time) ||
            !hits.IsInRange(time))
        {
            return;
        }

        var contactOffset = Center + movement * time - capCenter;
        var normal = contactOffset == Vector2.Zero
            ? Vector2.Zero
            : Vector2.Normalize(contactOffset);
        hits.Consider(time, normal);
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
