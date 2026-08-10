using Wrecs.Core;
using Wrecs.Geometry;

namespace Wrecs.Systems;

/// <summary>
/// Resolves circle movement against convex polygons and preserves movement
/// tangent to contacted polygon boundaries.
/// </summary>
public sealed class CircleConvexPolygonUpdateResolver :
    ISystemUpdateResolver,
    IRequire<CircleSystem>,
    IRequire<ConvexPolygonSystem>
{
    /// <summary>
    /// The distance retained between a resolved circle and a contacted polygon.
    /// </summary>
    public const float CollisionClearance = 0.001f;

    private CircleSystem? _circleSystem;
    private ConvexPolygonSystem? _convexPolygonSystem;

    public void Inject(CircleSystem dependency) => _circleSystem = dependency;

    public void Inject(ConvexPolygonSystem dependency) => _convexPolygonSystem = dependency;

    public ResolutionResult ResolveUpdates(UpdateSet proposedUpdateSet)
    {
        var circleSystem = _circleSystem ??
            throw new InvalidOperationException(
                $"{nameof(CircleSystem)} is required for {nameof(CircleConvexPolygonUpdateResolver)}");
        var convexPolygonSystem = _convexPolygonSystem ??
            throw new InvalidOperationException(
                $"{nameof(ConvexPolygonSystem)} is required for {nameof(CircleConvexPolygonUpdateResolver)}");

        var updates = proposedUpdateSet.Updates.ToArray();
        var polygonEntities = convexPolygonSystem.GetEntities();
        var resolutions = new Dictionary<IEntity, CircleResolution>();

        foreach (var update in updates.OfType<CircleUpdate>())
        {
            var start = circleSystem.GetTypedState(update.Entity).Circle;
            var destination = update.State.Circle;
            var requestedMovement = destination.Center - start.Center;
            var polygons = polygonEntities
                .Where(entity => entity != update.Entity)
                .Select(entity => convexPolygonSystem.GetTypedState(entity).Polygon);
            var allowedMovement = start.GetAllowedSlidingMovement(requestedMovement,
                                                                  polygons,
                                                                  CollisionClearance);
            var resolvedCenter = start.Center + allowedMovement;

            if (resolvedCenter == destination.Center)
                continue;

            resolutions[update.Entity] = new CircleResolution(
                destination with { Center = resolvedCenter },
                destination.Center - resolvedCenter);
        }

        if (resolutions.Count == 0)
            return new ResolutionResult(false, proposedUpdateSet);

        var resolvedUpdates = updates.Select(update =>
        {
            if (!resolutions.TryGetValue(update.Entity, out var resolution))
                return update;

            return update switch
            {
                CircleUpdate => new CircleUpdate(update.Entity, resolution.Circle),
                Spatial2DUpdate spatialUpdate => new Spatial2DUpdate(
                    update.Entity,
                    spatialUpdate.State.Position - resolution.BlockedMovement),
                _ => update
            };
        });

        return new ResolutionResult(true, new UpdateSet(resolvedUpdates));
    }

    private readonly record struct CircleResolution(Circle Circle,
                                                    Vector2 BlockedMovement);
}
