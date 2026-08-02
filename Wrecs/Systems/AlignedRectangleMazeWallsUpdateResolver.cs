using Wrecs.Core;
using Wrecs.Geometry;

namespace Wrecs.Systems;

/// <summary>
/// Resolves aligned-rectangle movement against axis-aligned maze walls.
/// </summary>
public sealed class AlignedRectangleMazeWallsUpdateResolver(
    IEnumerable<AxisAlignedSegment2> walls) :
    MazeWallsUpdateResolver<
        AlignedRectangleSystem,
        AlignedRectangleUpdate,
        AlignedRectangle>(walls)
{
    protected override AlignedRectangle GetCurrentCollider(AlignedRectangleSystem system,
                                                           IEntity entity) =>
        system.GetTypedState(entity).Rectangle;

    protected override AlignedRectangle GetDestinationCollider(AlignedRectangleUpdate update) =>
        update.State.Rectangle;

    protected override Vector2 GetPosition(AlignedRectangle collider) =>
        collider.BottomLeft;

    protected override AlignedRectangle SetPosition(AlignedRectangle collider, Vector2 position) =>
        collider with { BottomLeft = position };

    protected override Vector2 GetAllowedSlidingMovement(AlignedRectangle collider,
                                                         Vector2 requestedMovement,
                                                         IEnumerable<AxisAlignedSegment2> walls,
                                                         float clearance) =>
        collider.GetAllowedSlidingMovement(requestedMovement, walls, clearance);

    protected override IEntityUpdate CreateColliderUpdate(IEntity entity, AlignedRectangle collider) =>
        new AlignedRectangleUpdate(entity, collider);
}
