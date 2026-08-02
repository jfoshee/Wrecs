using Wrecs.Core;
using Wrecs.Geometry;

namespace Wrecs.Systems;

/// <summary>
/// Resolves circle movement against axis-aligned maze walls.
/// </summary>
public sealed class CircleMazeWallsUpdateResolver(
    IEnumerable<AxisAlignedSegment2> walls) :
    MazeWallsUpdateResolver<CircleSystem, CircleUpdate, Circle>(walls)
{
    protected override Circle GetCurrentCollider(CircleSystem system, IEntity entity) =>
        system.GetTypedState(entity).Circle;

    protected override Circle GetDestinationCollider(CircleUpdate update) =>
        update.State.Circle;

    protected override Vector2 GetPosition(Circle collider) => collider.Center;

    protected override Circle SetPosition(Circle collider, Vector2 position) =>
        collider with { Center = position };

    protected override Vector2 GetAllowedSlidingMovement(Circle collider,
                                                         Vector2 requestedMovement,
                                                         IEnumerable<AxisAlignedSegment2> walls,
                                                         float clearance) =>
        collider.GetAllowedSlidingMovement(requestedMovement, walls, clearance);

    protected override IEntityUpdate CreateColliderUpdate(IEntity entity, Circle collider) =>
        new CircleUpdate(entity, collider);
}
