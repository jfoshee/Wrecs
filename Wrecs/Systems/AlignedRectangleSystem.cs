using Wrecs.Core;
using Wrecs.Geometry;

namespace Wrecs.Systems;

public interface IAlignedRectangleEntity : IEntity;

public record struct AlignedRectangleSnapshot(AlignedRectangle Rectangle)
    : IStateSnapshot<AlignedRectangleSystem>
{
    public static implicit operator AlignedRectangle(AlignedRectangleSnapshot snapshot) =>
        snapshot.Rectangle;

    public static implicit operator AlignedRectangleSnapshot(AlignedRectangle rectangle) =>
        new(rectangle);
}

public record AlignedRectangleUpdate : EntityUpdate<AlignedRectangleSnapshot>
{
    public AlignedRectangleUpdate(IEntity entity, AlignedRectangle rectangle)
        : base(entity, new AlignedRectangleSnapshot(rectangle))
    {
    }
}

/// <summary>
/// Tracks an axis-aligned rectangle for each participating entity.
/// Entities without an initial rectangle use <see cref="AlignedRectangle.Empty"/>.
/// </summary>
public class AlignedRectangleSystem :
    ISystemWithDynamicEntities<IAlignedRectangleEntity, AlignedRectangleSnapshot>,
    ISystemAgentContextProvider<AlignedRectangleSnapshot>,
    ISystemAgentIntentTranslator<Move2DAction>,
    ISystemUpdateAcceptor<AlignedRectangleSnapshot>,
    ISystemLinkPositionSource,
    ISystemLinkPositionTarget
{
    private readonly Dictionary<IEntity, AlignedRectangle> _rectangles = [];

    public IReadOnlyList<IEntity> GetEntities() => [.. _rectangles.Keys];

    public AlignedRectangleSnapshot GetTypedState(IEntity entity) => new(_rectangles[entity]);

    public void InitEntities(params (IEntity entity, AlignedRectangleSnapshot? initialState)[] initialEntities)
    {
        _rectangles.Clear();
        foreach (var (entity, initialState) in initialEntities)
            AddEntity(entity, initialState);
    }

    public void AddEntity(IEntity entity, AlignedRectangleSnapshot? initialState) =>
        _rectangles[entity] = initialState ?? AlignedRectangle.Empty;

    public AlignedRectangleSnapshot? BuildSnapshot(IAgent agent) =>
        _rectangles.TryGetValue(agent, out var rectangle)
            ? new(rectangle)
            : null;

    public void ApplyUpdates(IEnumerable<EntityUpdate<AlignedRectangleSnapshot>> updates)
    {
        foreach (var update in updates)
            _rectangles[update.Entity] = update.State.Rectangle;
    }

    Vector2 ISystemLinkPositionSource.GetPosition(IEntity entity) =>
        _rectangles[entity].Center;

    void ISystemLinkPositionTarget.SetPosition(IEntity entity, Vector2 position)
    {
        AlignedRectangle rect = _rectangles[entity];
        if (rect.Center == position)
            return;
        _rectangles[entity] = AlignedRectangle.Centered(position, rect.Size);
    }

    public UpdateSet TranslateIntent(IAgent agent, Move2DAction action)
    {
        if (!_rectangles.TryGetValue(agent, out var rectangle))
            throw new InvalidOperationException($"Agent {agent.Name} does not have a rectangle in {nameof(AlignedRectangleSystem)}");

        var newRectangle = rectangle with
        {
            BottomLeft = rectangle.BottomLeft + action.Step
        };

        return new UpdateSet([new AlignedRectangleUpdate(agent, newRectangle)]);
    }
}
