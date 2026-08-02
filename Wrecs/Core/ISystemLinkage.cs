namespace Wrecs.Core;

public interface ISystemLinkPositionSource
{
    public Vector2 GetPosition(IEntity entity);
}

public interface ISystemLinkPositionTarget
{
    public void SetPosition(IEntity entity, Vector2 position);
}

/// <summary>
/// Represents a linkage between two entities, where the position of the source entity is used to override the position of the target entity.
/// </summary>
public record struct Linkage(IEntity SourceEntity,
                             IEntity TargetEntity,
                             ISystemLinkPositionSource SourceSystem,
                             ISystemLinkPositionTarget TargetSystem);
