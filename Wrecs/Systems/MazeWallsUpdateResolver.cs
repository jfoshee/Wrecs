using Wrecs.Core;
using Wrecs.Geometry;

namespace Wrecs.Systems;

// TODO: Perf: Only check nearby walls. The maze data structure could probably help.

/// <summary>
/// Shortens aligned-rectangle movement updates at the first maze wall they hit.
/// A paired spatial update for the same entity is shortened by the same amount.
/// </summary>
/// <remarks>
/// Creates a resolver for a fixed collection of axis-aligned maze walls.
/// </remarks>
public class MazeWallsUpdateResolver(IEnumerable<AxisAlignedSegment2> walls) :
    ISystemUpdateResolver,
    IRequire<AlignedRectangleSystem>
{
    /// <summary>
    /// The distance retained between a resolved rectangle and the wall it hit.
    /// </summary>
    public const float CollisionClearance = 0.001f;

    private readonly AxisAlignedSegment2[] _walls = [.. walls];
    private AlignedRectangleSystem? _alignedRectangleSystem;

    public void Inject(AlignedRectangleSystem dependency) =>
        _alignedRectangleSystem = dependency;

    public ResolutionResult ResolveUpdates(UpdateSet proposedUpdateSet)
    {
        var alignedRectangleSystem = _alignedRectangleSystem ??
            throw new InvalidOperationException(
                $"{nameof(AlignedRectangleSystem)} is required for {nameof(MazeWallsUpdateResolver)}");

        var updates = proposedUpdateSet.Updates.ToArray();
        var resolutions = new Dictionary<IEntity, RectangleResolution>();

        foreach (var update in updates.OfType<AlignedRectangleUpdate>())
        {
            var start = alignedRectangleSystem.GetTypedState(update.Entity).Rectangle;
            var destination = update.State.Rectangle;
            var requestedMovement = destination.BottomLeft - start.BottomLeft;
            var allowedMovement = start.GetAllowedSlidingMovement(
                requestedMovement,
                _walls,
                CollisionClearance);
            var resolvedBottomLeft = start.BottomLeft + allowedMovement;

            if (resolvedBottomLeft == destination.BottomLeft)
                continue;

            resolutions[update.Entity] = new RectangleResolution(
                destination with { BottomLeft = resolvedBottomLeft },
                destination.BottomLeft - resolvedBottomLeft);
        }

        if (resolutions.Count == 0)
            return new ResolutionResult(false, proposedUpdateSet);

        var resolvedUpdates = updates.Select(update =>
        {
            if (!resolutions.TryGetValue(update.Entity, out var resolution))
                return update;

            return update switch
            {
                AlignedRectangleUpdate =>
                    new AlignedRectangleUpdate(update.Entity, resolution.Rectangle),
                Spatial2DUpdate spatialUpdate =>
                    new Spatial2DUpdate(
                        update.Entity,
                        spatialUpdate.State.Position - resolution.BlockedMovement),
                _ => update
            };
        });

        return new ResolutionResult(true, new UpdateSet(resolvedUpdates));
    }

    private readonly record struct RectangleResolution(
        AlignedRectangle Rectangle,
        Vector2 BlockedMovement);
}
