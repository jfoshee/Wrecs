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
    ISystemWithEntities<IAlignedRectangleEntity, AlignedRectangleSnapshot>,
    ISystemAgentContextProvider<AlignedRectangleSnapshot>,
    ISystemUpdateAcceptor<AlignedRectangleSnapshot>
{
    private readonly Dictionary<IEntity, AlignedRectangle> _rectangles = [];

    public IReadOnlyList<IEntity> GetEntities() => [.. _rectangles.Keys];

    public AlignedRectangleSnapshot GetTypedState(IEntity entity) => new(_rectangles[entity]);

    public void InitEntities(params (IEntity entity, AlignedRectangleSnapshot? initialState)[] initialEntities)
    {
        _rectangles.Clear();
        foreach (var (entity, initialState) in initialEntities)
        {
            _rectangles[entity] = initialState ?? AlignedRectangle.Empty;
        }
    }

    public AlignedRectangleSnapshot? BuildSnapshot(IAgent agent) =>
        _rectangles.TryGetValue(agent, out var rectangle)
            ? new(rectangle)
            : null;

    public void ApplyUpdates(IEnumerable<EntityUpdate<AlignedRectangleSnapshot>> updates)
    {
        foreach (var update in updates)
            _rectangles[update.Entity] = update.State.Rectangle;
    }
}
