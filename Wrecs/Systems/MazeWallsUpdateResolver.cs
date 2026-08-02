using Wrecs.Core;
using Wrecs.Geometry;

namespace Wrecs.Systems;

/// <summary>
/// Resolves translated collider updates against a fixed collection of
/// axis-aligned maze walls and applies the blocked displacement to paired
/// spatial updates.
/// </summary>
public abstract class MazeWallsUpdateResolver<TSystem, TColliderUpdate, TCollider>(
    IEnumerable<AxisAlignedSegment2> walls) :
    ISystemUpdateResolver,
    IRequire<TSystem>
    where TSystem : class, ISystem
    where TColliderUpdate : class, IEntityUpdate
{
    /// <summary>
    /// The distance retained between a resolved collider and a contacted wall.
    /// </summary>
    public const float CollisionClearance = 0.001f;

    private readonly AxisAlignedSegment2[] _walls = [.. walls];
    private TSystem? _colliderSystem;

    public void Inject(TSystem dependency) => _colliderSystem = dependency;

    public ResolutionResult ResolveUpdates(UpdateSet proposedUpdateSet)
    {
        var colliderSystem = _colliderSystem ??
            throw new InvalidOperationException(
                $"{typeof(TSystem).Name} is required for {GetType().Name}");

        var updates = proposedUpdateSet.Updates.ToArray();
        var resolutions = new Dictionary<IEntity, ColliderResolution>();

        foreach (var update in updates.OfType<TColliderUpdate>())
        {
            var start = GetCurrentCollider(colliderSystem, update.Entity);
            var destination = GetDestinationCollider(update);
            var startPosition = GetPosition(start);
            var destinationPosition = GetPosition(destination);
            var requestedMovement = destinationPosition - startPosition;
            var allowedMovement = GetAllowedSlidingMovement(start,
                                                            requestedMovement,
                                                            _walls,
                                                            CollisionClearance);
            var resolvedPosition = startPosition + allowedMovement;

            if (resolvedPosition == destinationPosition)
                continue;

            resolutions[update.Entity] = new ColliderResolution(
                SetPosition(destination, resolvedPosition),
                destinationPosition - resolvedPosition);
        }

        if (resolutions.Count == 0)
            return new ResolutionResult(false, proposedUpdateSet);

        var resolvedUpdates = updates.Select(update =>
        {
            if (!resolutions.TryGetValue(update.Entity, out var resolution))
                return update;

            return update switch
            {
                TColliderUpdate => CreateColliderUpdate(update.Entity, resolution.Collider),
                Spatial2DUpdate spatialUpdate => new Spatial2DUpdate(
                    update.Entity,
                    spatialUpdate.State.Position - resolution.BlockedMovement),
                _ => update
            };
        });

        return new ResolutionResult(true, new UpdateSet(resolvedUpdates));
    }

    protected abstract TCollider GetCurrentCollider(TSystem system, IEntity entity);

    protected abstract TCollider GetDestinationCollider(TColliderUpdate update);

    protected abstract Vector2 GetPosition(TCollider collider);

    protected abstract TCollider SetPosition(TCollider collider, Vector2 position);

    protected abstract Vector2 GetAllowedSlidingMovement(TCollider collider,
                                                         Vector2 requestedMovement,
                                                         IEnumerable<AxisAlignedSegment2> walls,
                                                         float clearance);

    protected abstract IEntityUpdate CreateColliderUpdate(IEntity entity, TCollider collider);

    private readonly record struct ColliderResolution(TCollider Collider,
                                                      Vector2 BlockedMovement);
}
