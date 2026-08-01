using Wrecs.Core;
using Wrecs.Geometry;

namespace Wrecs.Systems;

/// <summary>
/// Shortens circle movement updates at maze walls and applies the same blocked
/// displacement to paired spatial updates.
/// </summary>
public class CircleMazeWallsUpdateResolver(IEnumerable<AxisAlignedSegment2> walls) :
    ISystemUpdateResolver,
    IRequire<CircleSystem>
{
    public const float CollisionClearance = 0.001f;

    private readonly AxisAlignedSegment2[] _walls = [.. walls];
    private CircleSystem? _circleSystem;

    public void Inject(CircleSystem dependency) => _circleSystem = dependency;

    public ResolutionResult ResolveUpdates(UpdateSet proposedUpdateSet)
    {
        var circleSystem = _circleSystem ??
            throw new InvalidOperationException(
                $"{nameof(CircleSystem)} is required for {nameof(CircleMazeWallsUpdateResolver)}");

        var updates = proposedUpdateSet.Updates.ToArray();
        var resolutions = new Dictionary<IEntity, CircleResolution>();

        foreach (var update in updates.OfType<CircleUpdate>())
        {
            var start = circleSystem.GetTypedState(update.Entity).Circle;
            var destination = update.State.Circle;
            var requestedMovement = destination.Center - start.Center;
            var allowedMovement = start.GetAllowedSlidingMovement(
                requestedMovement,
                _walls,
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

    private readonly record struct CircleResolution(
        Circle Circle,
        Vector2 BlockedMovement);
}
